using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class AffiliateRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/affiliate/api/public/affiliates/slug/{slug}", async (HttpContext ctx) =>
        {
            var slug = RequestUtil.Route(ctx, "slug") ?? "";
            var supported = DataStore.Response("SAC.json") as JsonArray;
            if (supported != null)
            {
                foreach (var codeNode in supported)
                {
                    var code = J.AsString(codeNode);
                    if (code != null && slug.Equals(code, StringComparison.OrdinalIgnoreCase))
                    {
                        await ResponseUtil.Json(ctx, new JsonObject
                        {
                            ["id"] = code,
                            ["slug"] = code,
                            ["displayName"] = code,
                            ["status"] = "ACTIVE",
                            ["verified"] = false
                        });
                        return;
                    }
                }
            }

            await ResponseUtil.Json(ctx, new JsonObject(), 404);
        });
    }
}
