// CMP 런 메타데이터(WaferRun)와 텔레메트리 샘플(Telemetry)을 SQLite에 저장/조회하는 저장소.
//   - ReserveRunAsync: 새 WaferSeq/RunId/WaferId 발급 및 중복 예약 처리
using System.Globalization;
using Microsoft.Data.Sqlite;
namespace Cmp.Collector.Services;

public sealed class SqliteStore : IAsyncDisposable
{
    private readonly string _cs;

    public SqliteStore(string dbPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(dbPath)) ?? ".");
        _cs = $"Data Source={dbPath};Cache=Shared;";
        EnsureInit();
    }

    private void EnsureInit()
    {
        using var con = new SqliteConnection(_cs);
        con.Open();
        using var cmd = con.CreateCommand();
        cmd.CommandText =
        """
        PRAGMA journal_mode=WAL;
        PRAGMA synchronous=NORMAL;

        CREATE TABLE IF NOT EXISTS WaferRun(
          RunKey     INTEGER PRIMARY KEY AUTOINCREMENT,
          WaferSeq   INTEGER NOT NULL UNIQUE,   -- 1,2,3... (중복 방지)
          RunId      TEXT    NOT NULL UNIQUE,   -- 프레임의 RunId (여기서는 W####와 동일 사용)
          WaferId    TEXT    NOT NULL,          -- "W0001"처럼 표시용 ID
          StartTs    TEXT    NOT NULL,          -- ISO8601 (UTC)
          InitMean   REAL,
          InitRange  REAL,
          InitWiw    REAL
        );

        CREATE TABLE IF NOT EXISTS Telemetry(
          RunId    TEXT NOT NULL,
          TSec     REAL NOT NULL,
          Ts       TEXT NOT NULL,       -- ISO8601(UTC)
          Seq      INTEGER,
          Phase    TEXT,
          DeviceId TEXT,
          Mean REAL, Wiw REAL, Rng REAL,
          P1 REAL, P2 REAL, P3 REAL, P4 REAL, P5 REAL,
          PRIMARY KEY (RunId, TSec),
          FOREIGN KEY (RunId) REFERENCES WaferRun(RunId)
        );

        CREATE INDEX IF NOT EXISTS IX_Telemetry_Run_T ON Telemetry(RunId, TSec);
        """;
        cmd.ExecuteNonQuery();
    }


    public async Task<(string runId, long waferSeq, string waferId, string startIsoUtc)>
    ReserveRunAsync()
{
    await using var con = new SqliteConnection(_cs);
    await con.OpenAsync();

    using var tx = con.BeginTransaction();

    // 1) 중복 호출 방지: "아직 프레임이 하나도 없는 최근 예약"이 있으면 그거 재사용
    string? reuseRunId = null;
    long reuseSeq = 0;
    string? reuseWaferId = null;
    string? reuseStartIso = null;

    await using (var cmd = con.CreateCommand())
    {
        cmd.Transaction = tx;
        cmd.CommandText =
        """
        SELECT wr.RunId, wr.WaferSeq, wr.WaferId, wr.StartTs
        FROM WaferRun wr
        WHERE
          wr.StartTs >= $threshUtc
          AND NOT EXISTS (SELECT 1 FROM Telemetry t WHERE t.RunId = wr.RunId)
        ORDER BY wr.RunKey DESC
        LIMIT 1;
        """;
        var thresh = DateTime.UtcNow.AddSeconds(-5) // 5초 내 재호출은 동일 런으로 간주
                        .ToString("o", CultureInfo.InvariantCulture);
        cmd.Parameters.AddWithValue("$threshUtc", thresh);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            reuseRunId   = reader.GetString(0);
            reuseSeq     = reader.GetInt64(1);
            reuseWaferId = reader.GetString(2);
            reuseStartIso= reader.GetString(3);
        }
    }

    if (reuseRunId != null)
    {
        tx.Commit();
        return (reuseRunId, reuseSeq, reuseWaferId!, reuseStartIso!);
    }

    // 2) 없다면 새 번호 발급 (연속 증가 보장)
    long last;
    await using (var cmd = con.CreateCommand())
    {
        cmd.Transaction = tx;
        cmd.CommandText = "SELECT IFNULL(MAX(WaferSeq),0) FROM WaferRun;";
        last = (long)(await cmd.ExecuteScalarAsync() ?? 0L);
    }

    long next = last + 1;
    string waferId = $"W{next:D4}";
    string runId   = waferId; // RunId = WaferId
    string nowIso  = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);

    await using (var cmd = con.CreateCommand())
    {
        cmd.Transaction = tx;
        cmd.CommandText =
        """
        INSERT INTO WaferRun(WaferSeq,RunId,WaferId,StartTs,InitMean,InitRange,InitWiw)
        VALUES ($seq,$run,$wafer,$ts,NULL,NULL,NULL);
        """;
        cmd.Parameters.AddWithValue("$seq",   next);
        cmd.Parameters.AddWithValue("$run",   runId);
        cmd.Parameters.AddWithValue("$wafer", waferId);
        cmd.Parameters.AddWithValue("$ts",    nowIso);
        await cmd.ExecuteNonQueryAsync();
    }

    tx.Commit();
    return (runId, next, waferId, nowIso);
}
    

    public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
