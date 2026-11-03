using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using LawinServer.Core;

namespace LawinServer.WebSockets;





public static class XmppServer
{
    private sealed class ClientData
    {
        public WebSocket Client { get; init; }
        public string AccountId { get; set; }
        public string Jid { get; set; }
        public string Id { get; set; }
        public PresenceUpdate LastPresenceUpdate { get; } = new();
    }

    private sealed class PresenceUpdate
    {
        public bool Away { get; set; }
        public string Status { get; set; } = "{}";
    }

    private static readonly List<ClientData> Clients = new();
    private static readonly object Gate = new();

    public static int Port => AppConfig.GetInt("Server", "xmppPort", 80);

    public static async Task HandleAsync(HttpContext ctx)
    {
        var requested = ctx.WebSockets.WebSocketRequestedProtocols;
        var isXmpp = requested.Any(p => p.Equals("xmpp", StringComparison.OrdinalIgnoreCase));

        WebSocket ws;
        if (isXmpp)
        {
            ws = await ctx.WebSockets.AcceptWebSocketAsync(new WebSocketAcceptContext { SubProtocol = "xmpp" });
        }
        else
        {
            ws = await ctx.WebSockets.AcceptWebSocketAsync();
        }

        if (!isXmpp)
        {
            await Matchmaker.RunAsync(ws);
            return;
        }

        await RunXmppSessionAsync(ws);
    }

    private static async Task RunXmppSessionAsync(WebSocket ws)
    {
        var accountId = "";
        var jid = "";
        var id = "";
        var streamId = Functions.MakeId();
        var authenticated = false;

        try
        {
            while (ws.State == WebSocketState.Open)
            {
                var message = await ReceiveTextAsync(ws);
                if (message == null) break;

                XDocument doc;
                try { doc = XDocument.Parse(message, LoadOptions.PreserveWhitespace); }
                catch { await ErrorAsync(ws); break; }

                var root = doc.Root;
                if (root == null) { await ErrorAsync(ws); break; }

                switch (root.Name.LocalName)
                {
                    case "open":
                        await SendAsync(ws, $"<open xmlns=\"urn:ietf:params:xml:ns:xmpp-framing\" from=\"prod.ol.epicgames.com\" id=\"{streamId}\" version=\"1.0\" xml:lang=\"en\"/>");

                        if (authenticated)
                        {
                            await SendAsync(ws,
                                "<stream:features xmlns:stream=\"http://etherx.jabber.org/streams\">" +
                                "<ver xmlns=\"urn:xmpp:features:rosterver\"/>" +
                                "<starttls xmlns=\"urn:ietf:params:xml:ns:xmpp-tls\"/>" +
                                "<bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"/>" +
                                "<compression xmlns=\"http://jabber.org/features/compress\"><method>zlib</method></compression>" +
                                "<session xmlns=\"urn:ietf:params:xml:ns:xmpp-session\"/>" +
                                "</stream:features>");
                        }
                        else
                        {
                            await SendAsync(ws,
                                "<stream:features xmlns:stream=\"http://etherx.jabber.org/streams\">" +
                                "<mechanisms xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\"><mechanism>PLAIN</mechanism></mechanisms>" +
                                "<ver xmlns=\"urn:xmpp:features:rosterver\"/>" +
                                "<starttls xmlns=\"urn:ietf:params:xml:ns:xmpp-tls\"/>" +
                                "<compression xmlns=\"http://jabber.org/features/compress\"><method>zlib</method></compression>" +
                                "<auth xmlns=\"http://jabber.org/features/iq-auth\"/>" +
                                "</stream:features>");
                        }
                        break;

                    case "auth":
                        {
                            var content = root.Value;
                            if (string.IsNullOrEmpty(content)) { await ErrorAsync(ws); return; }

                            string decoded;
                            try { decoded = Functions.DecodeBase64(content); }
                            catch { await ErrorAsync(ws); return; }

                            if (!decoded.Contains('\0')) { await ErrorAsync(ws); return; }
                            var decodedBase64 = decoded.Split('\0');

                            if (decodedBase64.Length != 3) { await ErrorAsync(ws); return; }

                            accountId = decodedBase64[1];
                            if (string.IsNullOrEmpty(accountId)) { await ErrorAsync(ws); return; }

                            lock (Gate)
                            {
                                if (Clients.Any(i => i.AccountId == accountId))
                                {
                                    
                                }
                            }

                            bool duplicate;
                            lock (Gate) duplicate = Clients.Any(i => i.AccountId == accountId);
                            if (duplicate) { await ErrorAsync(ws); return; }

                            authenticated = true;
                            Console.WriteLine($"An xmpp client with the account id {accountId} has logged in.");
                            await SendAsync(ws, "<success xmlns=\"urn:ietf:params:xml:ns:xmpp-sasl\"/>");
                        }
                        break;

                    case "iq":
                        {
                            var iqId = root.Attribute("id")?.Value;
                            switch (iqId)
                            {
                                case "_xmpp_bind1":
                                    {
                                        var bind = root.Elements().FirstOrDefault(e => e.Name.LocalName == "bind");
                                        var resource = bind?.Elements().FirstOrDefault(e => e.Name.LocalName == "resource")?.Value;
                                        if (resource == null) return;

                                        jid = $"{accountId}@prod.ol.epicgames.com/{resource}";
                                        id = $"{accountId}@prod.ol.epicgames.com";

                                        await SendAsync(ws,
                                            $"<iq to=\"{Attr(jid)}\" id=\"_xmpp_bind1\" xmlns=\"jabber:client\" type=\"result\">" +
                                            $"<bind xmlns=\"urn:ietf:params:xml:ns:xmpp-bind\"><jid>{Esc(jid)}</jid></bind></iq>");
                                    }
                                    break;

                                case "_xmpp_session1":
                                    if (!Exists(ws)) { await ErrorAsync(ws); return; }
                                    await SendAsync(ws,
                                        $"<iq to=\"{Attr(jid)}\" from=\"prod.ol.epicgames.com\" id=\"_xmpp_session1\" xmlns=\"jabber:client\" type=\"result\"/>");
                                    await GetPresenceFromAllAsync(ws);
                                    break;

                                default:
                                    if (!Exists(ws)) { await ErrorAsync(ws); return; }
                                    await SendAsync(ws,
                                        $"<iq to=\"{Attr(jid)}\" from=\"prod.ol.epicgames.com\" id=\"{Attr(iqId ?? "")}\" xmlns=\"jabber:client\" type=\"result\"/>");
                                    break;
                            }
                        }
                        break;

                    case "message":
                        {
                            if (!Exists(ws)) { await ErrorAsync(ws); return; }
                            var bodyNode = root.Elements().FirstOrDefault(e => e.Name.LocalName == "body");
                            if (bodyNode == null) break;
                            var body = bodyNode.Value;

                            if (root.Attribute("type")?.Value == "chat")
                            {
                                var to = root.Attribute("to")?.Value;
                                if (string.IsNullOrEmpty(to)) break;

                                ClientData receiver;
                                ClientData sender;
                                lock (Gate)
                                {
                                    receiver = Clients.FirstOrDefault(i => i.Id == to);
                                    sender = Clients.FirstOrDefault(i => i.Client == ws);
                                }
                                if (receiver == null || sender == null || receiver == sender) break;

                                await SendAsync(receiver.Client,
                                    $"<message to=\"{Attr(receiver.Jid)}\" from=\"{Attr(sender.Jid)}\" xmlns=\"jabber:client\" type=\"chat\"><body>{Esc(body)}</body></message>");
                                break;
                            }

                            if (IsJson(body))
                            {
                                JsonNode obj;
                                try { obj = J.Parse(body); } catch { obj = null; }
                                if (obj is JsonObject o && o.Get("type") is JsonValue tv && tv.TryGetValue<string>(out var type))
                                {
                                    switch (type.ToLowerInvariant())
                                    {
                                        case "com.epicgames.party.invitation":
                                            {
                                                var to = root.Attribute("to")?.Value;
                                                if (string.IsNullOrEmpty(to)) break;

                                                ClientData sender;
                                                ClientData receiver;
                                                lock (Gate)
                                                {
                                                    sender = Clients.FirstOrDefault(i => i.Client == ws);
                                                    receiver = Clients.FirstOrDefault(i => i.Id == to);
                                                }
                                                if (receiver == null || sender == null) break;

                                                await SendAsync(receiver.Client,
                                                    $"<message from=\"{Attr(sender.Jid)}\" id=\"{Attr(root.Attribute("id")?.Value ?? "")}\" to=\"{Attr(receiver.Jid)}\" xmlns=\"jabber:client\"><body>{Esc(body)}</body></message>");
                                            }
                                            break;

                                        default:
                                            await SendAsync(ws,
                                                $"<message from=\"{Attr(jid)}\" id=\"{Attr(root.Attribute("id")?.Value ?? "")}\" to=\"{Attr(jid)}\" xmlns=\"jabber:client\"><body>{Esc(body)}</body></message>");
                                            break;
                                    }
                                }
                            }
                        }
                        break;

                    case "presence":
                        {
                            if (!Exists(ws)) { await ErrorAsync(ws); return; }
                            var statusNode = root.Elements().FirstOrDefault(e => e.Name.LocalName == "status");
                            if (statusNode == null) break;
                            if (!IsJson(statusNode.Value)) break;

                            var body = statusNode.Value;
                            var away = root.Elements().Any(e => e.Name.LocalName == "show");
                            await UpdatePresenceForAllAsync(ws, body, away, false);
                        }
                        break;
                }

                if (!Exists(ws) && !string.IsNullOrEmpty(accountId) && !string.IsNullOrEmpty(jid) &&
                    !string.IsNullOrEmpty(streamId) && !string.IsNullOrEmpty(id) && authenticated)
                {
                    lock (Gate)
                    {
                        if (!Clients.Any(i => i.Client == ws))
                        {
                            Clients.Add(new ClientData
                            {
                                Client = ws,
                                AccountId = accountId,
                                Jid = jid,
                                Id = id
                            });
                        }
                    }
                }
            }
        }
        catch
        {
            
        }
        finally
        {
            await RemoveClientAsync(ws);
        }
    }

    private static bool Exists(WebSocket ws)
    {
        lock (Gate) return Clients.Any(i => i.Client == ws);
    }

    private static async Task RemoveClientAsync(WebSocket ws)
    {
        ClientData data;
        lock (Gate) data = Clients.FirstOrDefault(i => i.Client == ws);

        if (data != null)
        {
            await UpdatePresenceForAllAsync(ws, "{}", false, true);
            Console.WriteLine($"An xmpp client with the account id {data.AccountId} has logged out.");
            lock (Gate) Clients.RemoveAll(i => i.Client == ws);
        }
    }

    private static async Task UpdatePresenceForAllAsync(WebSocket ws, string body, bool away, bool offline)
    {
        ClientData sender;
        lock (Gate) sender = Clients.FirstOrDefault(i => i.Client == ws);
        if (sender == null) { await ErrorAsync(ws); return; }

        sender.LastPresenceUpdate.Away = away;
        sender.LastPresenceUpdate.Status = body;

        List<ClientData> recipients;
        lock (Gate) recipients = Clients.ToList();

        foreach (var clientData in recipients)
        {
            var xml = $"<presence to=\"{Attr(clientData.Jid)}\" xmlns=\"jabber:client\" from=\"{Attr(sender.Jid)}\" type=\"{(offline ? "unavailable" : "available")}\">";
            if (away) xml += $"<show>away</show><status>{Esc(body)}</status>";
            else xml += $"<status>{Esc(body)}</status>";
            xml += "</presence>";

            try { await SendAsync(clientData.Client, xml); } catch { }
        }
    }

    private static async Task GetPresenceFromAllAsync(WebSocket ws)
    {
        ClientData sender;
        lock (Gate) sender = Clients.FirstOrDefault(i => i.Client == ws);
        if (sender == null) { await ErrorAsync(ws); return; }

        List<ClientData> clients;
        lock (Gate) clients = Clients.ToList();

        foreach (var clientData in clients)
        {
            var xml = $"<presence to=\"{Attr(sender.Jid)}\" xmlns=\"jabber:client\" from=\"{Attr(clientData.Jid)}\" type=\"available\">";
            if (clientData.LastPresenceUpdate.Away)
                xml += $"<show>away</show><status>{Esc(clientData.LastPresenceUpdate.Status)}</status>";
            else
                xml += $"<status>{Esc(clientData.LastPresenceUpdate.Status)}</status>";
            xml += "</presence>";

            try { await SendAsync(sender.Client, xml); } catch { }
        }
    }

    public static void Broadcast(string body)
    {
        List<ClientData> clients;
        lock (Gate) clients = Clients.ToList();

        foreach (var clientData in clients)
        {
            var xml = $"<message from=\"xmpp-admin@prod.ol.epicgames.com\" xmlns=\"jabber:client\" to=\"{Attr(clientData.Jid)}\"><body>{Esc(body)}</body></message>";
            try { _ = SendAsync(clientData.Client, xml); } catch { }
        }
    }

    private static async Task ErrorAsync(WebSocket ws)
    {
        try { await SendAsync(ws, "<close xmlns=\"urn:ietf:params:xml:ns:xmpp-framing\"/>"); } catch { }
        try
        {
            if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
        }
        catch { }
    }

    private static async Task<string> ReceiveTextAsync(WebSocket ws)
    {
        var buffer = new byte[8192];
        using var ms = new MemoryStream();
        while (true)
        {
            WebSocketReceiveResult result;
            try { result = await ws.ReceiveAsync(buffer, CancellationToken.None); }
            catch { return null; }

            if (result.MessageType == WebSocketMessageType.Close) return null;
            ms.Write(buffer, 0, result.Count);
            if (result.EndOfMessage) break;
        }
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static Task SendAsync(WebSocket ws, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        return ws.SendAsync(bytes, WebSocketMessageType.Text, true, CancellationToken.None);
    }

    private static bool IsJson(string str)
    {
        try { JsonNode.Parse(str); return true; } catch { return false; }
    }

    private static string Esc(string value) =>
        (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string Attr(string value) =>
        Esc(value).Replace("\"", "&quot;");
}
