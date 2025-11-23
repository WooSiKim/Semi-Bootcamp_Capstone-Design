// Unity에서 들어오는 텔레메트리 JSON 스트림을 runId별로 누적하여
// CMP 한 런(웨이퍼)당 집계 결과를 SummaryCsvWriter를 통해 CSV 한 줄로 기록하는 컴포넌트.
using System;
using System.Collections.Concurrent;
using System.Text.Json;
using Cmp.Collector.Services;

public sealed class RunSummaryAggregator
{
    private readonly SummaryCsvWriter _csv;

    // runId별 누적 스냅샷
    private readonly ConcurrentDictionary<string, Acc> _acc =
        new ConcurrentDictionary<string, Acc>();

    // 중복 DONE 프레임 방지
    private readonly ConcurrentDictionary<string, byte> _finished =
        new ConcurrentDictionary<string, byte>();

    public RunSummaryAggregator(SummaryCsvWriter csv)
    {
        if (csv == null) throw new ArgumentNullException(nameof(csv));
        _csv = csv;
    }

    // 유니티에서 한 줄(json) 들어올 때마다 호출
    public void HandleLine(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return;

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(json);
        }
        catch
        {
            return; // 잘못된 JSON 무시
        }

        using (doc)
        {
            var root = doc.RootElement;

            // 스키마 체크
            string? schema = null;
            JsonElement s;
            if (root.TryGetProperty("Schema", out s) && s.ValueKind == JsonValueKind.String)
                schema = s.GetString();
            if (schema != "cmp.telemetry.v1") return; // 원래 스키마 유지

            // runId
            string? runId = "";
            JsonElement ridEl;
            if (root.TryGetProperty("RunId", out ridEl) && ridEl.ValueKind == JsonValueKind.String)
                runId = ridEl.GetString() ?? "";
            if (string.IsNullOrEmpty(runId)) return;

            // ★ 프레임에서 WaferId 추출 (우선순위: Wafer.WaferId → WaferId → 없음)
            string? waferId = null;
            JsonElement wEl;
            if (root.TryGetProperty("Wafer", out wEl) && wEl.ValueKind == JsonValueKind.Object)
            {
                JsonElement widEl;
                if (wEl.TryGetProperty("WaferId", out widEl) && widEl.ValueKind == JsonValueKind.String)
                    waferId = widEl.GetString();
            }
            else
            {
                JsonElement wid2El;
                if (root.TryGetProperty("WaferId", out wid2El) && wid2El.ValueKind == JsonValueKind.String)
                    waferId = wid2El.GetString();
            }

            // 타임스탬프
            DateTime ts = DateTime.UtcNow;
            JsonElement tsEl;
            if (root.TryGetProperty("Ts", out tsEl) && tsEl.ValueKind == JsonValueKind.String)
            {
                DateTime tmp;
                if (DateTime.TryParse(tsEl.GetString(), out tmp)) ts = tmp;
            }

            // 종료 플래그 & 상태
            bool isDone = false;
            string? endStatus = null;
            JsonElement flags;
            if (root.TryGetProperty("Flags", out flags) && flags.ValueKind == JsonValueKind.Object)
            {
                JsonElement dEl;
                if (flags.TryGetProperty("IsDone", out dEl) && dEl.ValueKind == JsonValueKind.True)
                {
                    isDone = dEl.GetBoolean();
                }

                JsonElement esEl;
                if (isDone && flags.TryGetProperty("EndStatus", out esEl) && esEl.ValueKind == JsonValueKind.String)
                    endStatus = esEl.GetString();
            }

            // 메트릭
            double mean = 0, range = 0, wiw = 0;
            JsonElement metrics;
            if (root.TryGetProperty("Metrics", out metrics) && metrics.ValueKind == JsonValueKind.Object)
            {
                JsonElement m;
                if (metrics.TryGetProperty("Mean", out m)) mean = SafeDouble(m);
                JsonElement r;
                if (metrics.TryGetProperty("Range", out r)) range = SafeDouble(r);
                JsonElement w;
                if (metrics.TryGetProperty("Wiw", out w)) wiw = SafeDouble(w);
            }

            // 경과 시간
            double elapsed = 0;
            JsonElement e;
            if (root.TryGetProperty("ElapsedSec", out e)) elapsed = SafeDouble(e);

            // 누적 객체
            var acc = _acc.GetOrAdd(runId, _ => new Acc { RunId = runId });

            // 최초 프레임에서 스타트/초기값/웨이퍼ID 고정
            if (!acc.HasStart)
            {
                acc.StartUtc = ts;
                acc.MeanIn = mean;
                acc.RangeIn = range;
                acc.WiwIn = wiw;
                acc.HasStart = true;

                if (!string.IsNullOrWhiteSpace(waferId))
                    acc.WaferId = waferId;
            }
            else
            {
                // 뒤늦게라도 WaferId가 오면 채워둠(초기 프레임에 비어왔던 경우 대비)
                if (string.IsNullOrWhiteSpace(acc.WaferId) && !string.IsNullOrWhiteSpace(waferId))
                    acc.WaferId = waferId;
            }

            // 최신값 갱신
            acc.MeanOut = mean;
            acc.RangeOut = range;
            acc.WiwOut = wiw;
            acc.ElapsedSec = elapsed;

            // 종료 처리
            if (isDone)
            {
                // 같은 runId로 중복 DONE 들어오면 1회만 기록
                if (!_finished.TryAdd(runId, 1))
                {
                    _acc.TryRemove(runId, out _);
                    return;
                }

                acc.Status = string.IsNullOrEmpty(endStatus) ? "ABORT" : endStatus;

                // ★ 핵심: CSV id는 WaferId 우선, 없으면 RunId
                string idForCsv = string.IsNullOrWhiteSpace(acc.WaferId) ? acc.RunId : acc.WaferId;

                _csv.Append(new SummaryCsvWriter.Row(
                    idForCsv,
                    acc.MeanIn, acc.RangeIn, acc.WiwIn,
                    acc.MeanOut, acc.RangeOut, acc.WiwOut,
                    acc.StartUtc, acc.ElapsedSec,
                    acc.Status
                ));

                // 정리
                _acc.TryRemove(runId, out _);
            }
        }
    }

    private static double SafeDouble(JsonElement el)
    {
        try { return el.GetDouble(); }
        catch { return 0; }
    }

    private sealed class Acc
    {
        public string RunId = "";
        public string WaferId = "";   // CSV의 id로 사용 (없으면 RunId)
        public bool HasStart;
        public DateTime StartUtc;
        public double MeanIn, RangeIn, WiwIn;
        public double MeanOut, RangeOut, WiwOut;
        public double ElapsedSec;
        public string Status = "";
    }
}
