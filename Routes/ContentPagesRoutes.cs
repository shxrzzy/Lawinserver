using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class ContentPagesRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/content/api/pages/fortnite-game/spark-tracks", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("Athena/sparkTracks.json").DeepClone()));

        app.MapGet("/content/api/pages/fortnite-game/radio-stations", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["_title"] = "Radio Stations",
                ["radioStationList"] = new JsonObject
                {
                    ["_type"] = "RadioStationList",
                    ["stations"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["resourceID"] = "QWGQAynCdixzoLIdJl",
                            ["stationImage"] = "https://fortnite-public-service-prod11.ol.epicgames.com/images/lawinpfp.png",
                            ["_type"] = "RadioStationItem",
                            ["title"] = new JsonObject
                            {
                                ["ar"] = "الحفل الملكي",
                                ["de"] = "Party Royale",
                                ["en"] = "Party Royale",
                                ["es"] = "Fiesta magistral",
                                ["es-419"] = "Fiesta campal",
                                ["fr"] = "Fête royale",
                                ["it"] = "Party Reale",
                                ["ja"] = "パーティーロイヤル",
                                ["ko"] = "파티로얄",
                                ["pl"] = "Królewska Impreza",
                                ["pt-BR"] = "Festa Royale",
                                ["ru"] = "Королевская вечеринка",
                                ["tr"] = "Çılgın Parti",
                                ["zh-CN"] = "空降派对",
                                ["zh-Hant"] = "空降派對"
                            }
                        }
                    }
                },
                ["_noIndex"] = false,
                ["_activeDate"] = "2024-06-13T10:00:00.000Z",
                ["lastModified"] = "2024-06-12T20:12:56.271Z",
                ["_locale"] = "en-US",
                ["_templateName"] = "FortniteGameRadioStations",
                ["_suggestedPrefetch"] = new JsonArray()
            }));

        app.MapGet("/content/api/pages/fortnite-game/seasonpasses", async (HttpContext ctx) =>
        {
            var seasonPasses = (JsonObject)DataStore.Response("Athena/seasonPasses.json").DeepClone();
            Functions.ChooseTranslationsInJson(seasonPasses, ctx);
            await ResponseUtil.Json(ctx, seasonPasses);
        });

        app.MapGet("/content/api/pages/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, Functions.GetContentPages(ctx)));

        app.MapPost("/api/v1/fortnite-br/{**rest}", async (HttpContext ctx) =>
        {
            var motd = (JsonObject)DataStore.Response("Athena/motd.json").DeepClone();
            var body = await RequestUtil.ReadObjectAsync(ctx);
            var language = body.GetStr("language") ?? body["parameters"]?.GetStr("language") ?? "";
            Functions.ChooseTranslationsInJson(motd, ctx, language);

            if (body["tags"] is JsonArray tags)
            {
                if (motd["contentItems"] is JsonArray contentItems)
                {
                    foreach (var item in contentItems)
                    {
                        if (item is not JsonObject o) continue;
                        var placements = new JsonArray();
                        foreach (var tag in tags)
                        {
                            placements.Add(new JsonObject
                            {
                                ["trackingId"] = "lawinstrackingidlol",
                                ["tag"] = J.AsString(tag),
                                ["position"] = 0
                            });
                        }
                        o["placements"] = placements;
                    }
                }
            }

            await ResponseUtil.Json(ctx, motd);
        });
    }
}
