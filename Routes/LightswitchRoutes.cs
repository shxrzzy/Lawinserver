using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class LightswitchRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/lightswitch/api/service/Fortnite/status", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["serviceInstanceId"] = "fortnite",
                ["status"] = "UP",
                ["message"] = "Fortnite is online",
                ["maintenanceUri"] = null,
                ["overrideCatalogIds"] = new JsonArray { "a7f138b2e51945ffbfdacc1af0541053" },
                ["allowedActions"] = new JsonArray(),
                ["banned"] = false,
                ["launcherInfoDTO"] = new JsonObject
                {
                    ["appName"] = "Fortnite",
                    ["catalogItemId"] = "4fe75bbc5a674f4f9b356b5c90567da5",
                    ["namespace"] = "fn"
                }
            }));

        app.MapGet("/lightswitch/api/service/bulk/status", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray
            {
                new JsonObject
                {
                    ["serviceInstanceId"] = "fortnite",
                    ["status"] = "UP",
                    ["message"] = "fortnite is up.",
                    ["maintenanceUri"] = null,
                    ["overrideCatalogIds"] = new JsonArray { "a7f138b2e51945ffbfdacc1af0541053" },
                    ["allowedActions"] = new JsonArray { "PLAY", "DOWNLOAD" },
                    ["banned"] = false,
                    ["launcherInfoDTO"] = new JsonObject
                    {
                        ["appName"] = "Fortnite",
                        ["catalogItemId"] = "4fe75bbc5a674f4f9b356b5c90567da5",
                        ["namespace"] = "fn"
                    }
                }
            }));
    }
}
