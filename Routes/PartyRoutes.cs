using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class PartyRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/party/api/v1/Fortnite/user/{accountId}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["current"] = new JsonArray(),
                ["pending"] = new JsonArray(),
                ["invites"] = new JsonArray(),
                ["pings"] = new JsonArray()
            }));

        app.MapPost("/party/api/v1/Fortnite/parties", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadObjectAsync(ctx);
            var joinInfo = body["join_info"] as JsonObject;
            if (joinInfo == null) { await ResponseUtil.Json(ctx, new JsonObject()); return; }
            var connection = joinInfo["connection"] as JsonObject;
            if (connection == null) { await ResponseUtil.Json(ctx, new JsonObject()); return; }

            var config = new JsonObject { ["type"] = "DEFAULT" };
            if (body["config"] is JsonObject incomingConfig)
                foreach (var kv in incomingConfig) config[kv.Key] = kv.Value?.DeepClone();
            config["discoverability"] = "ALL";
            config["sub_type"] = "default";
            config["invite_ttl"] = 14400;
            config["intention_ttl"] = 60;

            var connectionId = connection.GetStr("id", "");
            var accountId = connectionId.Split("@prod")[0];
            var now = Functions.IsoNow();

            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["id"] = Functions.MakeId().Replace("-", ""),
                ["created_at"] = now,
                ["updated_at"] = now,
                ["config"] = config,
                ["members"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["account_id"] = accountId,
                        ["meta"] = joinInfo["meta"]?.DeepClone() ?? new JsonObject(),
                        ["connections"] = new JsonArray
                        {
                            new JsonObject
                            {
                                ["id"] = connectionId,
                                ["connected_at"] = now,
                                ["updated_at"] = now,
                                ["yield_leadership"] = false,
                                ["meta"] = connection["meta"]?.DeepClone() ?? new JsonObject()
                            }
                        },
                        ["revision"] = 0,
                        ["updated_at"] = now,
                        ["joined_at"] = now,
                        ["role"] = "CAPTAIN"
                    }
                },
                ["applicants"] = new JsonArray(),
                ["meta"] = body["meta"]?.DeepClone() ?? new JsonObject(),
                ["invites"] = new JsonArray(),
                ["revision"] = 0,
                ["intentions"] = new JsonArray()
            });
        });

        app.MapMethods("/party/api/v1/Fortnite/parties/{**rest}",
            new[] { "GET", "POST", "PUT", "DELETE", "PATCH" },
            async (HttpContext ctx) => await ResponseUtil.Empty(ctx, 204));
    }
}
