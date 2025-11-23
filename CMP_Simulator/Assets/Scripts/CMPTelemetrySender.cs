// CMPTelemetrySender.cs — 교체본
using System;
using System.Text;                        // ★ Encoding.UTF8
using System.Collections;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;              // ★ WS 전송
using UnityEngine;
using UnityEngine.Networking;             // ★ UnityWebRequest

public class CMPTelemetrySender : MonoBehaviour
{
    [Header("Collector endpoints")]
    public string baseUrl = "http://127.0.0.1:8080";
    public string wsUrl = "ws://127.0.0.1:8080/ingest/ws";

    [Header("Sampling")]
    public float samplePeriodSec = 0.2f;

    [Header("Source")]
    public CMPDriver driver;

    // state
    private string _runId = "";
    private string _waferId = "";
    private long _seq = 0;

    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private bool _wsLoopStarted = false;
    private bool _sendLoopStarted = false;
    private bool _sentDoneOnce = false;

    void Awake()
    {
        if (driver == null) driver = FindObjectOfType<CMPDriver>();
    }

    void OnEnable()
    {
        _cts = new CancellationTokenSource();
        if (!_wsLoopStarted)
        {
            _wsLoopStarted = true;
            StartCoroutine(CoWsLoop());
        }
    }

    void OnDisable()
    {
        try { _cts?.Cancel(); } catch { }
        try { _ws?.Dispose(); } catch { }
        _wsLoopStarted = false;
        _sendLoopStarted = false;
    }

    // ★ 로봇팔이 웨이퍼를 '집었을 때' 1회만 호출
    public void NotifyWaferPicked()
    {
        StartCoroutine(CoReserveAndMaybeStartSend());
    }

    // (선택) 사이클 끝났을 때 초기화용
    public void NotifyCycleEnded()
    {
        Debug.Log("[TX] cycle ended → reset runId");
        _runId = "";
        _waferId = "";
        _seq = 0;
        _sendLoopStarted = false;
        _sentDoneOnce = false;               // ★ 추가
    }


    // ── /run/reserve 호출 → runId/waferId 확보 ──
    IEnumerator CoReserveAndMaybeStartSend()
    {
        if (!string.IsNullOrEmpty(_runId))
        {
            Debug.Log("[TX] already reserved, skipping");
            yield break;
        }

        string url = baseUrl.TrimEnd('/') + "/run/reserve";
        Debug.Log($"[TX] >>> reserve request to {url}");

        using (var req = UnityWebRequest.Post(url, new WWWForm()))
        {
            yield return req.SendWebRequest();

            Debug.Log($"[TX] <<< reserve result: {req.result} code={req.responseCode} error={req.error}");
            Debug.Log($"[TX] <<< body: {req.downloadHandler.text}");

            if (req.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning($"[TX] reserve failed: {req.error}");
                yield break;
            }

            string body = req.downloadHandler.text ?? "{}";
            _runId = Extract(body, "\"runId\"");
            _waferId = Extract(body, "\"waferId\"");
            Debug.Log($"[TX] parsed runId={_runId} waferId={_waferId}");

            if (string.IsNullOrEmpty(_runId))
            {
                Debug.LogWarning($"[TX] reserve parse failed: {body}");
                yield break;
            }

            if (string.IsNullOrEmpty(_waferId)) _waferId = _runId;

            _seq = 0;
            Debug.Log($"[TX] reserve OK → runId={_runId} waferId={_waferId}");
            _sentDoneOnce = false;    
            // reserve 실패 시(또는 응답 파싱 실패 시) 로컬로 runId 생성해서 계속 진행
            if (string.IsNullOrEmpty(_runId))
            {
                var now = DateTime.UtcNow;
                _runId = "U" + now.ToString("yyyyMMdd-HHmmss-fff");
                _waferId = _runId;
                Debug.LogWarning($"[TX] reserve missing → fallback runId={_runId}");
            }

        }
    }

    // ── WS 연결 유지 ──
    IEnumerator CoWsLoop()
    {
        while (_cts != null && !_cts.IsCancellationRequested)
        {
            _ws = new ClientWebSocket();
            var task = _ws.ConnectAsync(new Uri(wsUrl), _cts.Token);
            while (!task.IsCompleted) yield return null;

            if (_ws.State == WebSocketState.Open)
                Debug.Log("[TX] WS connected");

            while (_cts != null && !_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
                yield return new WaitForSeconds(0.5f);

            try { _ws?.Dispose(); } catch { }
            yield return new WaitForSeconds(1f);
        }
    }

    // ── 주기 송신 ──
    IEnumerator CoSendLoop()
{
    var wait = new WaitForSeconds(samplePeriodSec > 0 ? samplePeriodSec : 0.2f);
    while (_cts != null && !_cts.IsCancellationRequested)
    {
        if (_ws != null && _ws.State == WebSocketState.Open && !string.IsNullOrEmpty(_runId))
        {
            var f = BuildFrame(Time.realtimeSinceStartup);

            // ★ DONE이면 1번만 보내고, 그 다음 DONE 프레임들은 무시
            if (f.Flags != null && f.Flags.IsDone)
            {
                if (_sentDoneOnce)
                {
                    yield return wait;
                    continue;            // 추가 DONE 프레임 무시
                }
            }

            var line = JsonUtility.ToJson(f) + "\n";
            var seg = new ArraySegment<byte>(Encoding.UTF8.GetBytes(line));
            var send = _ws.SendAsync(seg, WebSocketMessageType.Text, true, CancellationToken.None);
            while (!send.IsCompleted) yield return null;

            if (f.Flags != null && f.Flags.IsDone)
                _sentDoneOnce = true;   // 첫 DONE 전송 이후 플래그 잠금

            _seq++;
        }
        yield return wait;
    }
}

    // ── 프레임 구성 (필요치 외 값은 0/기본) ──
    private TelemetryFrameLike BuildFrame(double elapsedSec)
    {
        var f = new TelemetryFrameLike
        {
            Schema = "cmp.telemetry.v1",
            DeviceId = SystemInfo.deviceName ?? "unity",
            RunId = _runId,
            Seq = _seq,
            Ts = DateTime.UtcNow.ToString("o"),
            ElapsedSec = elapsedSec,
            Phase = "Unknown",
            SampleRateHz = (samplePeriodSec > 0) ? (1.0 / samplePeriodSec) : 5.0,
            Metrics = new TelemetryFrameLike.MetricsBlock
            {
                Mean = 0,
                Wiw = 0,
                Range = 0,
                Ec = 0,
                RpmP = 0,
                RpmW = 0,
                Slurry = 0,
                Pressures = new double[] { 0, 0, 0, 0, 0 }
            },
            Flags = new TelemetryFrameLike.FlagsBlock
            {
                IsRunning = false,
                IsDone = false,
                HasWafer = false
            },
            Wafer = new TelemetryFrameLike.WaferBlock
            {
                WaferId = _waferId,
                LotId = ""
            }
        };

        if (driver != null)
        {
            try { f.Phase = driver.GetCurrentStateName(); } catch { }

            try
            {
                float[] Z; float mean, sigma, range, ec;
                driver.Measure(out Z, out mean, out sigma, out range, out ec);
                f.Metrics.Mean = mean;
                f.Metrics.Range = range;
                f.Metrics.Wiw = driver.ComputeWIWNU(range, mean);

                float[] cur, cmd; driver.GetPressures(out cur, out cmd);
                f.Metrics.Pressures = new double[] { cmd[0], cmd[1], cmd[2], cmd[3], cmd[4] };
            }
            catch { }

            try
            {
                var ph = f.Phase ?? "IDLE";
                f.Flags.IsRunning = (ph == "RUN");
                f.Flags.IsDone = (ph == "DONE");
                f.Flags.HasWafer = (ph != "IDLE");

                // ★ Done이면, 드라이버에서 종료상태 받아 채우기
                if (f.Flags.IsDone && driver != null)
                {
                    // driver.GetEndStatus(): NORMAL/ABORT
                    var endStatus = "ABORT";
                    try { endStatus = driver.GetEndStatus(); } catch { }
                    f.Flags.EndStatus = endStatus; // Collector가 그대로 CSV에 기록
                }
            }
            catch { }

            try { f.ElapsedSec = driver.GetElapsedTime(); } catch { }
        }

        return f;
    }

    private static string Extract(string json, string key)
    {
        int i = json.IndexOf(key, StringComparison.Ordinal);
        if (i < 0) return "";
        i = json.IndexOf(':', i); if (i < 0) return "";
        int q1 = json.IndexOf('"', i + 1); if (q1 < 0) return "";
        int q2 = json.IndexOf('"', q1 + 1); if (q2 < 0) return "";
        return json.Substring(q1 + 1, q2 - q1 - 1);
    }

    public void NotifyPolishStart()
    {
        if (!_sendLoopStarted)
        {
            Debug.Log("[TX] CMP process started → begin telemetry stream");
            _sendLoopStarted = true;
            StartCoroutine(CoSendLoop());
        }
    }

    public void ReserveNowIfNeeded()
{
    // 같은 사이클 중 중복 예약 방지
    if (!string.IsNullOrEmpty(_runId)) return;
    StartCoroutine(CoReserveAndMaybeStartSend());
}


    [Serializable]
    private class TelemetryFrameLike
    {
        public string Schema, DeviceId, RunId, Ts, Phase;
        public long Seq;
        public double ElapsedSec, SampleRateHz;
        public MetricsBlock Metrics;
        public FlagsBlock Flags;
        public WaferBlock Wafer;

        [Serializable]
        public class MetricsBlock
        { public double Mean, Wiw, Range, Ec, RpmP, RpmW; public int Slurry; public double[] Pressures; }
        [Serializable]
        public class FlagsBlock
        {
            public bool IsRunning, IsDone, HasWafer;
            public string EndStatus;
        }
        [Serializable]
        public class WaferBlock
        { public string LotId, WaferId; }
    }
}