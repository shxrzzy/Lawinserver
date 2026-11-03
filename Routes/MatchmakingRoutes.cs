using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class MatchmakingRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/fortnite/api/matchmaking/session/findPlayer/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 200));

        app.MapGet("/fortnite/api/game/v2/matchmakingservice/ticket/player/{**rest}", async (HttpContext ctx) =>
        {
            var bucketId = RequestUtil.Query(ctx, "bucketId") ?? "";
            var buildId = bucketId.Split(':')[0];
            ctx.Response.Cookies.Append("currentbuildUniqueId", buildId);

            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["serviceUrl"] = "ws://127.0.0.1",
                ["ticketType"] = "mms-player",
                ["payload"] = "69=",
                ["signature"] = "420="
            });
        });

        app.MapGet("/fortnite/api/game/v2/matchmaking/account/{accountId}/session/{sessionId}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["accountId"] = RequestUtil.Route(ctx, "accountId"),
                ["sessionId"] = RequestUtil.Route(ctx, "sessionId"),
                ["key"] = "AOJEv8uTFmUh7XM2328kq9rlAzeQ5xzWzPIiyKn2s7s="
            }));

        app.MapGet("/fortnite/api/matchmaking/session/{sessionId}", async (HttpContext ctx) =>
        {
            var buildUniqueId = ctx.Request.Cookies["currentbuildUniqueId"] ?? "0";
            var ip = AppConfig.Get("GameServer", "ip", "127.0.0.1");
            var port = AppConfig.GetInt("GameServer", "port", 7777);

            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["id"] = RequestUtil.Route(ctx, "sessionId"),
                ["ownerId"] = Functions.MakeId().Replace("-", "").ToUpperInvariant(),
                ["ownerName"] = "[DS]fortnite-liveeugcec1c2e30ubrcore0a-z8hj-1968",
                ["serverName"] = "[DS]fortnite-liveeugcec1c2e30ubrcore0a-z8hj-1968",
                ["serverAddress"] = ip,
                ["serverPort"] = port,
                ["maxPublicPlayers"] = 220,
                ["openPublicPlayers"] = 175,
                ["maxPrivatePlayers"] = 0,
                ["openPrivatePlayers"] = 0,
                ["attributes"] = new JsonObject
                {
                    ["REGION_s"] = "EU",
                    ["GAMEMODE_s"] = "FORTATHENA",
                    ["ALLOWBROADCASTING_b"] = true,
                    ["SUBREGION_s"] = "GB",
                    ["DCID_s"] = "FORTNITE-LIVEEUGCEC1C2E30UBRCORE0A-14840880",
                    ["tenant_s"] = "Fortnite",
                    ["MATCHMAKINGPOOL_s"] = "Any",
                    ["STORMSHIELDDEFENSETYPE_i"] = 0,
                    ["HOTFIXVERSION_i"] = 0,
                    ["PLAYLISTNAME_s"] = "Playlist_DefaultSolo",
                    ["SESSIONKEY_s"] = Functions.MakeId().Replace("-", "").ToUpperInvariant(),
                    ["TENANT_s"] = "Fortnite",
                    ["BEACONPORT_i"] = 15009
                },
                ["publicPlayers"] = new JsonArray(),
                ["privatePlayers"] = new JsonArray(),
                ["totalPlayers"] = 45,
                ["allowJoinInProgress"] = false,
                ["shouldAdvertise"] = false,
                ["isDedicated"] = false,
                ["usesStats"] = false,
                ["allowInvites"] = false,
                ["usesPresence"] = false,
                ["allowJoinViaPresence"] = true,
                ["allowJoinViaPresenceFriendsOnly"] = false,
                ["buildUniqueId"] = buildUniqueId,
                ["lastUpdated"] = Functions.IsoNow(),
                ["started"] = false
            });
        });

        app.MapPost("/fortnite/api/matchmaking/session/{sessionId}/join", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));

        app.MapPost("/fortnite/api/matchmaking/session/matchMakingRequest", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));
    }
}
