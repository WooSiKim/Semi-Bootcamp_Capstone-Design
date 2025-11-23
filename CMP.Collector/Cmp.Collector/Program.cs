//Unity CMP 시뮬레이터에서 전송하는 텔레메트리 JSON을 수신하여
//실시간 뷰어로 WebSocket 브로드캐스트하고
//런 단위 집계 결과를 CSV/SQLite에 기록하는 최소 웹 서버 엔트리포인트.
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cmp.Collector.Services;
using System.Threading.Channels;

var builder = WebApplication.CreateBuilder(args);

// ─────────────────────────────────────────────────────────────
// 설정: CSV 루트 (없으면 logs 폴더 생성)
var csvRoot = builder.Configuration["storage:csvRoot"]
             ?? Path.Combine(AppContext.BaseDirectory, "logs");

// DI 등록
builder.Services.AddSingleton(new SummaryCsvWriter(csvRoot));   // CSV 라이터
var ingest = Channel.CreateUnbounded<string>();                 // 문자열(JSON) 인입 채널
builder.Services.AddSingleton(ingest);
builder.Services.AddSingleton<RunSummaryAggregator>();          // 런 요약 집계 워커
builder.Services.AddSingleton<BroadcastHub>();

builder.Services.AddSingleton(new SqliteStore(Path.Combine(csvRoot, "cmp.sqlite3")));



var app = builder.Build();

app.MapPost("/run/reserve", async (SqliteStore db) =>
{
    var (runId, _, waferId, _) = await db.ReserveRunAsync();
    return Results.Json(new { runId, waferId }); // runId=W####, waferId=W####
});
// 라이브 팬아웃
app.UseWebSockets();

// DI에서 참조
var hub = app.Services.GetRequiredService<BroadcastHub>();

// ─────────────────────────────────────────────────────────────
// 0) Health
app.MapGet("/health", () => Results.Ok("ok"));

// 1) Unity → 수신 WebSocket (0.2s 프레임, '\n' 단위)
app.Map("/ingest/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var buffer = new byte[64 * 1024];
    var sb = new StringBuilder();

    while (ws.State == WebSocketState.Open)
    {
        var res = await ws.ReceiveAsync(buffer, CancellationToken.None);
        if (res.MessageType == WebSocketMessageType.Close) break;

        sb.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));

        while (TryReadLine(sb, out var line))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            // ① 라이브 브로드캐스트(뷰어)
            await hub.LiveChannel.Writer.WriteAsync(line);

            // ② CSV 요약 집계(런 종료 시 1줄 append)
            await ingest.Writer.WriteAsync(line);
        }
    }
});

// 2) 배치 수집(옵션): JSON 배열로 여러 프레임 한 번에
app.MapPost("/ingest/batch", async (HttpContext ctx) =>
{
    using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
    var body = await sr.ReadToEndAsync();

    try
    {
        using var json = JsonDocument.Parse(body);
        if (json.RootElement.TryGetProperty("frames", out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var el in arr.EnumerateArray())
            {
                var line = el.GetRawText(); // 원문 유지
                await hub.LiveChannel.Writer.WriteAsync(line);
                await ingest.Writer.WriteAsync(line);
            }
            return Results.Ok(new { ok = true, count = arr.GetArrayLength() });
        }
    }
    catch
    {
        // 필요 시 로깅
    }

    return Results.BadRequest(new { ok = false });
});

// 3) Viewer(WinForms) → 라이브 WebSocket 구독
app.Map("/live/ws", async ctx =>
{
    if (!ctx.WebSockets.IsWebSocketRequest)
    {
        ctx.Response.StatusCode = 400;
        return;
    }

    using var ws = await ctx.WebSockets.AcceptWebSocketAsync();
    var id = hub.AddClient(ws);

    try
    {
        var pingBuf = new byte[1];
        while (ws.State == WebSocketState.Open)
        {
            var res = await ws.ReceiveAsync(pingBuf, CancellationToken.None);
            if (res.MessageType == WebSocketMessageType.Close) break;
        }
    }
    finally
    {
        hub.RemoveClient(id);
        try { await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None); } catch { }
    }
});

// 4) 백그라운드 팬아웃 루프
var cts = new CancellationTokenSource();
_ = Task.Run(() => hub.FanoutLoopAsync(cts.Token));

var agg = app.Services.GetRequiredService<RunSummaryAggregator>();
var ingestChan = app.Services.GetRequiredService<Channel<string>>();
_ = Task.Run(() => AggregateLoop(ingestChan, agg, cts.Token));

app.Lifetime.ApplicationStopping.Register(() => cts.Cancel());

await app.RunAsync();

// ─────────────────────────────────────────────────────────────
static bool TryReadLine(StringBuilder sb, out string? line)
{
    for (int i = 0; i < sb.Length; i++)
    {
        if (sb[i] == '\n')
        {
            line = sb.ToString(0, i).TrimEnd('\r');
            sb.Remove(0, i + 1);
            return true;
        }
    }
    line = null;
    return false;
}
static async Task AggregateLoop(Channel<string> ingest, RunSummaryAggregator agg, CancellationToken ct)
{
    var reader = ingest.Reader;
    while (!ct.IsCancellationRequested)
    {
        string line;
        try
        {
            line = await reader.ReadAsync(ct);
        }
        catch (OperationCanceledException)
        {
            break;
        }
        catch
        {
            continue; // 손상된 라인/예외 무시
        }

        try
        {
            agg.HandleLine(line);
        }
        catch
        {
            // 필요시 로깅
        }
    }
}

