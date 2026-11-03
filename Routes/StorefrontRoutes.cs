using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class StorefrontRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/fortnite/api/storefront/v2/catalog", async (HttpContext ctx) =>
        {
            if (RequestUtil.UserAgent(ctx).Contains("2870186"))
            {
                await ResponseUtil.Empty(ctx, 404);
                return;
            }

            var catalog = Functions.GetItemShop();
            var memory = Functions.GetVersionInfo(ctx);

            if (memory.Build >= 30.10)
                catalog = J.Parse(catalog.ToJsonString(J.Compact).Replace("\"Normal\"", "\"Size_1_x_2\"")) as JsonObject;

            if (memory.Build >= 30.20)
                catalog = J.Parse(catalog.ToJsonString(J.Compact).Replace("Game/Items/CardPacks/", "SaveTheWorld/Items/CardPacks/")) as JsonObject;

            await ResponseUtil.Json(ctx, catalog);
        });

        app.MapGet("/fortnite/api/storefront/v2/keychain", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("keychain.json").DeepClone()));

        app.MapGet("/catalog/api/shared/bulk/offers", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));
    }
}
