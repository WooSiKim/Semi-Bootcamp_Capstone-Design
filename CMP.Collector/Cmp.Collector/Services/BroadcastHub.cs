// Collector 내부에서 라이브 텔레메트리 문자열을 모든 뷰어 클라이언트에 팬아웃하는 허브.
//   - Producer: Program.cs의 /ingest/ws, /ingest/batch 에서 LiveChannel에 JSON 라인 push
//   - Consumer: FanoutLoopAsync가 LiveChannel에서 읽어 모든 WebSocket 클라이언트에 전송
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Channels;

namespace Cmp.Collector.Services;

public sealed class BroadcastHub
{
    // 실시간 메시지 팬아웃용 채널(문자열 = 원본 JSON 라인)
    public Channel<string> LiveChannel { get; } = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = false });

    // 구독자(라이브 뷰어) 관리
    private readonly ConcurrentDictionary<Guid, WebSocket> _clients = new();

    public Guid AddClient(WebSocket ws)
    {
        var id = Guid.NewGuid();
        _clients[id] = ws;
        return id;
    }

    public void RemoveClient(Guid id)
    {
        _clients.TryRemove(id, out _);
    }

    public async Task FanoutLoopAsync(CancellationToken ct)
    {
        var reader = LiveChannel.Reader;
        var encoding = Encoding.UTF8;

        while (!ct.IsCancellationRequested && await reader.WaitToReadAsync(ct))
        {
            while (reader.TryRead(out var msg))
            {
                var data = encoding.GetBytes(msg + "\n");
                foreach (var kv in _clients.ToArray())
                {
                    var ws = kv.Value;
                    if (ws.State != WebSocketState.Open)
                    {
                        RemoveClient(kv.Key);
                        continue;
                    }
                    try
                    {
                        await ws.SendAsync(data, WebSocketMessageType.Text, true, ct);
                    }
                    catch
                    {
                        RemoveClient(kv.Key);
                    }
                }
            }
        }
    }
}
