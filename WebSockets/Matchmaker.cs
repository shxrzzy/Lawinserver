using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.WebSockets;


public static class Matchmaker
{
    public static async Task RunAsync(WebSocket ws)
    {
        var ticketId = Functions.MakeId().Replace("-", "");
        var matchId = Functions.MakeId().Replace("-", "");
        var sessionId = Functions.MakeId().Replace("-", "");

        try
        {
            await SendAsync(ws, new JsonObject
            {
                ["payload"] = new JsonObject { ["state"] = "Connecting" },
                ["name"] = "StatusUpdate"
            });
            await Task.Delay(800);

            await SendAsync(ws, new JsonObject
            {
                ["payload"] = new JsonObject
                {
                    ["totalPlayers"] = 1,
                    ["connectedPlayers"] = 1,
                    ["state"] = "Waiting"
                },
                ["name"] = "StatusUpdate"
            });
            await Task.Delay(1000);

            await SendAsync(ws, new JsonObject
            {
                ["payload"] = new JsonObject
                {
                    ["ticketId"] = ticketId,
                    ["queuedPlayers"] = 0,
                    ["estimatedWaitSec"] = 0,
                    ["status"] = new JsonObject(),
                    ["state"] = "Queued"
                },
                ["name"] = "StatusUpdate"
            });
            await Task.Delay(4000);

            await SendAsync(ws, new JsonObject
            {
                ["payload"] = new JsonObject
                {
                    ["matchId"] = matchId,
                    ["state"] = "SessionAssignment"
                },
                ["name"] = "StatusUpdate"
            });
            await Task.Delay(2000);

            await SendAsync(ws, new JsonObject
            {
                ["payload"] = new JsonObject
                {
                    ["matchId"] = matchId,
                    ["sessionId"] = sessionId,
                    ["joinDelaySec"] = 1
                },
                ["name"] = "Play"
            });

            
            var buffer = new byte[1024];
            while (ws.State == WebSocketState.Open)
            {
                var result = await ws.ReceiveAsync(buffer, CancellationToken.None);
                if (result.MessageType == WebSocketMessageType.Close) break;
            }
        }
        catch { }
        finally
        {
            try
            {
                if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                    await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
            }
            catch { }
        }
    }

    private static Task SendAsync(WebSocket ws, JsonNode node)
    {
        var bytes = Encoding.UTF8.GetBytes(node.ToJsonString(J.Compact));
        return ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }
}
