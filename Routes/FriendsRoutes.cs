using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class FriendsRoutes
{
    private static JsonArray FriendList => DataStore.Response("friendslist.json") as JsonArray ?? new JsonArray();
    private static JsonObject FriendList2 => DataStore.Response("friendslist2.json") as JsonObject ?? new JsonObject();

    public static void Map(WebApplication app)
    {
        app.MapGet("/friends/api/v1/{accountId}/settings", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));

        app.MapGet("/friends/api/v1/{accountId}/blocklist", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapGet("/friends/api/public/friends/{accountId}", async (HttpContext ctx) =>
        {
            var memory = Functions.GetVersionInfo(ctx);
            var accountId = RequestUtil.Route(ctx, "accountId");
            var friends = FriendList;
            var friends2 = FriendList2;

            if (!friends.Any(n => n is JsonObject o && o.GetStr("accountId") == accountId))
            {
                var created = Functions.IsoNow();
                var friendObject = new JsonObject
                {
                    ["accountId"] = accountId,
                    ["status"] = "ACCEPTED",
                    ["direction"] = "OUTBOUND",
                    ["created"] = created,
                    ["favorite"] = false
                };

                friends.Add(friendObject);
                if (friends2["friends"] is not JsonArray list) { list = new JsonArray(); friends2["friends"] = list; }
                list.Add(new JsonObject
                {
                    ["accountId"] = accountId,
                    ["groups"] = new JsonArray(),
                    ["mutual"] = 0,
                    ["alias"] = "",
                    ["note"] = "",
                    ["favorite"] = false,
                    ["created"] = created
                });

                Functions.SendXmppMessageToAll(new JsonObject
                {
                    ["payload"] = friendObject.DeepClone(),
                    ["type"] = "com.epicgames.friends.core.apiobjects.Friend",
                    ["timestamp"] = created
                });

                Functions.SendXmppMessageToAll(new JsonObject
                {
                    ["type"] = "FRIENDSHIP_REQUEST",
                    ["timestamp"] = created,
                    ["from"] = accountId,
                    ["status"] = "ACCEPTED"
                });

                DataStore.SaveResponse("friendslist.json", friends);
                DataStore.SaveResponse("friendslist2.json", friends2);
            }

            if (memory.Season >= 7)
            {
                var result = (JsonArray)friends.DeepClone();
                for (var i = result.Count - 1; i >= 0; i--)
                {
                    if (result[i] is JsonObject o && o.GetStr("accountId") == accountId)
                        result.RemoveAt(i);
                }
                await ResponseUtil.Json(ctx, result);
                return;
            }

            await ResponseUtil.Json(ctx, friends);
        });

        app.MapGet("/friends/api/v1/{accountId}/summary", async (HttpContext ctx) =>
        {
            var accountId = RequestUtil.Route(ctx, "accountId");
            var friends2 = FriendList2;
            var friends = FriendList;

            if (friends2["friends"] is not JsonArray list) { list = new JsonArray(); friends2["friends"] = list; }

            if (!list.Any(n => n is JsonObject o && o.GetStr("accountId") == accountId))
            {
                var created = Functions.IsoNow();
                var friendObject = new JsonObject
                {
                    ["accountId"] = accountId,
                    ["groups"] = new JsonArray(),
                    ["mutual"] = 0,
                    ["alias"] = "",
                    ["note"] = "",
                    ["favorite"] = false,
                    ["created"] = created
                };

                list.Add(friendObject);
                friends.Add(new JsonObject
                {
                    ["accountId"] = accountId,
                    ["status"] = "ACCEPTED",
                    ["direction"] = "OUTBOUND",
                    ["created"] = created,
                    ["favorite"] = false
                });

                Functions.SendXmppMessageToAll(new JsonObject
                {
                    ["payload"] = new JsonObject
                    {
                        ["accountId"] = accountId,
                        ["status"] = "ACCEPTED",
                        ["direction"] = "OUTBOUND",
                        ["created"] = created,
                        ["favorite"] = false
                    },
                    ["type"] = "com.epicgames.friends.core.apiobjects.Friend",
                    ["timestamp"] = created
                });

                Functions.SendXmppMessageToAll(new JsonObject
                {
                    ["type"] = "FRIENDSHIP_REQUEST",
                    ["timestamp"] = created,
                    ["from"] = accountId,
                    ["status"] = "ACCEPTED"
                });

                DataStore.SaveResponse("friendslist.json", friends);
                DataStore.SaveResponse("friendslist2.json", friends2);
            }

            await ResponseUtil.Json(ctx, friends2);
        });

        app.MapGet("/friends/api/public/list/fortnite/{accountId}/recentPlayers", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapGet("/friends/api/public/blocklist/{accountId}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject { ["blockedUsers"] = new JsonArray() }));
    }
}
