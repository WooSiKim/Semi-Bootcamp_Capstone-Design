// Collector의 /live/ws WebSocket에 접속해 텔레메트리 JSON 스트림을 수신하는 클라이언트.
//   - 백그라운드 루프에서 문자열을 수신/역직렬화하고,
//   - 각 TelemetryFrame을 OnFrame 이벤트로 MainForm에 전달한다.
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Cmp.Viewer.Win.Models;

namespace Cmp.Viewer.Win.Services
{
    public sealed class WsSubscriber : IAsyncDisposable
    {
        private readonly Uri _wsUri;
        private ClientWebSocket? _ws;
        private CancellationTokenSource? _cts;

        public event Action<TelemetryFrame>? OnFrame;

        public WsSubscriber(string wsUrl) => _wsUri = new Uri(wsUrl);

        public Task StartAsync()
        {
            _cts = new CancellationTokenSource();
            _ = Task.Run(() => LoopAsync(_cts.Token));
            return Task.CompletedTask;
        }

        public async Task StopAsync()
        {
            try { _cts?.Cancel(); } catch { }
            if (_ws != null && _ws.State == WebSocketState.Open)
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            _ws?.Dispose();
        }

        private async Task LoopAsync(CancellationToken ct)
        {
            var buffer = new byte[64 * 1024];

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    _ws = new ClientWebSocket();
                    await _ws.ConnectAsync(_wsUri, ct);

                    while (!ct.IsCancellationRequested && _ws.State == WebSocketState.Open)
                    {
                        var sb = new StringBuilder();
                        WebSocketReceiveResult res;
                        do
                        {
                            res = await _ws.ReceiveAsync(buffer, ct);
                            if (res.MessageType == WebSocketMessageType.Close)
                                goto NextReconnect;
                            if (res.MessageType != WebSocketMessageType.Text)
                                continue;

                            sb.Append(Encoding.UTF8.GetString(buffer, 0, res.Count));
                        }
                        while (!res.EndOfMessage);

                        // 메시지 1건 완성 (개행 없을 수도 있음)
                        var payload = sb.ToString();

                        // 개행이 있다면 여러 줄로 쪼개 처리
                        foreach (var line in payload.Split('\n'))
                        {
                            var txt = line.Trim();
                            if (string.IsNullOrWhiteSpace(txt)) continue;

                            try
                            {
                                var f = JsonSerializer.Deserialize<TelemetryFrame>(txt);
                                if (f != null) OnFrame?.Invoke(f);
                            }
                            catch
                            {
                                // 파싱 실패는 무시 (원하면 로깅)
                            }
                        }
                    }
                NextReconnect:;
                }
                catch
                {
                    await Task.Delay(1000, ct); // 재연결
                }
                finally
                {
                    try { _ws?.Dispose(); } catch { }
                }
            }
        }

        public async ValueTask DisposeAsync() => await StopAsync();
    }
}
