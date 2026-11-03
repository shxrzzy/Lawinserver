using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class PrivacyRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/fortnite/api/game/v2/privacy/account/{accountId}", async (HttpContext ctx) =>
        {
            var privacy = DataStore.Response("privacy.json") as JsonObject ?? new JsonObject();
            privacy["accountId"] = RequestUtil.Route(ctx, "accountId");
            await ResponseUtil.Json(ctx, privacy);
        });

        app.MapPost("/fortnite/api/game/v2/privacy/account/{accountId}", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadObjectAsync(ctx);
            var privacy = DataStore.Response("privacy.json") as JsonObject ?? new JsonObject();
            privacy["accountId"] = RequestUtil.Route(ctx, "accountId");
            privacy["optOutOfPublicLeaderboards"] = body["optOutOfPublicLeaderboards"]?.DeepClone() ?? false;
            DataStore.SaveResponse("privacy.json", privacy);
            await ResponseUtil.Json(ctx, privacy);
        });
    }
}
