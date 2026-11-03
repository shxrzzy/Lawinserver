using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class DiscoveryRoutes
{
    public static void Map(WebApplication app)
    {
        var discovery = DataStore.Response("Athena/Discovery/discovery_frontend.json") as JsonObject;
        var v2 = discovery?["v2"]?.DeepClone();
        var v1 = discovery?["v1"]?.DeepClone();

        app.MapPost("/api/v2/discovery/surface/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, v2));
        app.MapPost("/fortnite/api/v2/discovery/surface/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, v2));
        app.MapPost("/discovery/surface/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, v1));
        app.MapPost("/fortnite/api/discovery/surface/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, v1));

        app.MapGet("/fortnite/api/discovery/accessToken/{branch}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["branchName"] = RequestUtil.Route(ctx, "branch"),
                ["appId"] = "Fortnite",
                ["token"] = "lawinstokenlol"
            }));

        app.MapPost("/links/api/fn/mnemonic", async (HttpContext ctx) =>
        {
            var arr = new JsonArray();
            if (v2?["Panels"]?[1]?["Pages"]?[0]?["results"] is JsonArray results)
            {
                foreach (var result in results)
                {
                    var linkData = result?["linkData"];
                    if (linkData != null) arr.Add(linkData.DeepClone());
                }
            }
            await ResponseUtil.Json(ctx, arr);
        });

        app.MapGet("/links/api/fn/mnemonic/{playlist}/related", async (HttpContext ctx) =>
        {
            var response = new JsonObject
            {
                ["parentLinks"] = new JsonArray(),
                ["links"] = new JsonObject()
            };

            var playlist = RequestUtil.Route(ctx, "playlist");
            if (!string.IsNullOrEmpty(playlist) && v2?["Panels"]?[1]?["Pages"]?[0]?["results"] is JsonArray results)
            {
                foreach (var result in results)
                {
                    var linkData = result?["linkData"] as JsonObject;
                    if (linkData != null && linkData.GetStr("mnemonic") == playlist)
                        ((JsonObject)response["links"])[playlist] = linkData.DeepClone();
                }
            }

            await ResponseUtil.Json(ctx, response);
        });

        app.MapGet("/links/api/fn/mnemonic/{**rest}", async (HttpContext ctx) =>
        {
            var path = RequestUtil.Route(ctx, "rest") ?? "";
            var mnemonic = path.Split('/').LastOrDefault() ?? "";
            if (v2?["Panels"]?[1]?["Pages"]?[0]?["results"] is JsonArray results)
            {
                foreach (var result in results)
                {
                    var linkData = result?["linkData"] as JsonObject;
                    if (linkData != null && linkData.GetStr("mnemonic") == mnemonic)
                    {
                        await ResponseUtil.Json(ctx, linkData.DeepClone());
                        return;
                    }
                }
            }
            await ResponseUtil.Empty(ctx, 404);
        });

        app.MapPost("/api/v1/links/lock-status/{accountId}/check", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadObjectAsync(ctx);
            var response = new JsonObject
            {
                ["results"] = new JsonArray(),
                ["hasMore"] = false
            };

            if (body["linkCodes"] is JsonArray linkCodes)
            {
                foreach (var linkCode in linkCodes)
                {
                    ((JsonArray)response["results"]).Add(new JsonObject
                    {
                        ["playerId"] = RequestUtil.Route(ctx, "accountId"),
                        ["linkCode"] = J.AsString(linkCode),
                        ["lockStatus"] = "UNLOCKED",
                        ["lockStatusReason"] = "NONE",
                        ["isVisible"] = true
                    });
                }
            }

            await ResponseUtil.Json(ctx, response);
        });
    }
}
