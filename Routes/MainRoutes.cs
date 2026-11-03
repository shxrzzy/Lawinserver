using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class MainRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapGet("/clearitemsforshop", async (HttpContext ctx) =>
        {
            var athena = ProfileStore.Get("athena");
            var config = DataStore.CatalogConfig() as JsonObject;
            var changed = false;

            if (config != null)
            {
                foreach (var pair in config)
                {
                    if (pair.Value is not JsonObject cfg) continue;
                    if (cfg["itemGrants"] is not JsonArray grants) continue;

                    foreach (var grantNode in grants)
                    {
                        var grant = J.AsString(grantNode);
                        if (string.IsNullOrEmpty(grant)) continue;

                        var items = athena["items"] as JsonObject;
                        if (items == null) continue;

                        foreach (var itemKey in items.Select(kv => kv.Key).ToList())
                        {
                            var templateId = items[itemKey]?.GetStr("templateId") ?? "";
                            if (grant.Equals(templateId, StringComparison.OrdinalIgnoreCase))
                            {
                                items.Remove(itemKey);
                                changed = true;
                            }
                        }
                    }
                }
            }

            if (changed)
            {
                athena["rvn"] = athena.GetLong("rvn") + 1;
                athena["commandRevision"] = athena.GetLong("commandRevision") + 1;
                ProfileStore.Save("athena", athena);
                await ResponseUtil.Text(ctx, "Success");
            }
            else
            {
                await ResponseUtil.Text(ctx, "Failed, there are no items to remove");
            }
        });

        app.MapGet("/eulatracking/api/shared/agreements/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));

        app.MapGet("/fortnite/api/game/v2/friendcodes/{code}/epic", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray
            {
                new JsonObject { ["codeId"] = "L4WINS3RV3R", ["codeType"] = "CodeToken:FounderFriendInvite", ["dateCreated"] = "2024-04-02T21:37:00.420Z" },
                new JsonObject { ["codeId"] = "playeereq", ["codeType"] = "CodeToken:FounderFriendInvite_XBOX", ["dateCreated"] = "2024-04-02T21:37:00.420Z" },
                new JsonObject { ["codeId"] = "lawinscodelol", ["codeType"] = "CodeToken:MobileInvite", ["dateCreated"] = "2024-04-02T21:37:00.420Z" }
            }));

        app.MapGet("/launcher/api/public/distributionpoints/", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["distributions"] = new JsonArray
                {
                    "https://epicgames-download1.akamaized.net/",
                    "https://download.epicgames.com/",
                    "https://download2.epicgames.com/",
                    "https://download3.epicgames.com/",
                    "https://download4.epicgames.com/",
                    "https://lawinserver.ol.epicgames.com/"
                }
            }));

        app.MapGet("/launcher/api/public/assets/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["appName"] = "FortniteContentBuilds",
                ["labelName"] = "LawinServer",
                ["buildVersion"] = "++Fortnite+Release-20.00-CL-19458861-Windows",
                ["catalogItemId"] = "5cb97847cee34581afdbc445400e2f77",
                ["expires"] = "9999-12-31T23:59:59.999Z",
                ["items"] = new JsonObject
                {
                    ["MANIFEST"] = new JsonObject
                    {
                        ["signature"] = "LawinServer",
                        ["distribution"] = "https://lawinserver.ol.epicgames.com/",
                        ["path"] = "Builds/Fortnite/Content/CloudDir/LawinServer.manifest",
                        ["hash"] = "55bb954f5596cadbe03693e1c06ca73368d427f3",
                        ["additionalDistributions"] = new JsonArray()
                    },
                    ["CHUNKS"] = new JsonObject
                    {
                        ["signature"] = "LawinServer",
                        ["distribution"] = "https://lawinserver.ol.epicgames.com/",
                        ["path"] = "Builds/Fortnite/Content/CloudDir/LawinServer.manifest",
                        ["additionalDistributions"] = new JsonArray()
                    }
                },
                ["assetId"] = "FortniteContentBuilds"
            }));

        app.MapGet("/Builds/Fortnite/Content/CloudDir/{**path}", async (HttpContext ctx) =>
        {
            var path = RequestUtil.Route(ctx, "path") ?? "";
            if (path.EndsWith(".manifest", StringComparison.OrdinalIgnoreCase))
            {
                var data = await File.ReadAllBytesAsync(Paths.Combine("responses", "CloudDir", "LawinServer.manifest"));
                await ResponseUtil.Bytes(ctx, data);
                return;
            }
            if (path.EndsWith(".chunk", StringComparison.OrdinalIgnoreCase))
            {
                var data = await File.ReadAllBytesAsync(Paths.Combine("responses", "CloudDir", "LawinServer.chunk"));
                await ResponseUtil.Bytes(ctx, data);
                return;
            }
            if (path.EndsWith(".ini", StringComparison.OrdinalIgnoreCase))
            {
                var data = await File.ReadAllBytesAsync(Paths.Combine("responses", "CloudDir", "Full.ini"));
                await ResponseUtil.Bytes(ctx, data);
                return;
            }
            await ResponseUtil.Empty(ctx, 404);
        });

        app.MapPost("/fortnite/api/game/v2/grant_access/{**rest}", async (HttpContext ctx) =>
        {
            await ResponseUtil.Json(ctx, new JsonObject());
        });

        app.MapPost("/api/v1/user/setting", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapGet("/waitingroom/api/waitingroom", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));

        app.MapGet("/socialban/api/public/v1/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["bans"] = new JsonArray(),
                ["warnings"] = new JsonArray()
            }));

        app.MapGet("/fortnite/api/game/v2/events/tournamentandhistory/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("Athena/Tournament/tournamentandhistory.json").DeepClone()));

        app.MapGet("/fortnite/api/statsv2/account/{accountId}", async (HttpContext ctx) =>
            await StatsResponse(ctx));
        app.MapGet("/statsproxy/api/statsv2/account/{accountId}", async (HttpContext ctx) =>
            await StatsResponse(ctx));
        app.MapGet("/fortnite/api/stats/accountId/{accountId}/bulk/window/alltime", async (HttpContext ctx) =>
            await StatsResponse(ctx));

        app.MapGet("/d98eeaac-2bfa-4bf4-8a59-bdc95469c693", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["playlist"] = "PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPE1QRCB4bWxucz0idXJuOm1wZWc6ZGFzaDpzY2hlbWE6bXBkOjIwMTEiIHhtbG5zOnhzaT0iaHR0cDovL3d3dy53My5vcmcvMjAwMS9YTUxTY2hlbWEtaW5zdGFuY2UiIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIiB4c2k6c2NoZW1hTG9jYXRpb249InVybjptcGVnOkRBU0g6c2NoZW1hOk1QRDoyMDExIGh0dHA6Ly9zdGFuZGFyZHMuaXNvLm9yZy9pdHRmL1B1YmxpY2x5QXZhaWxhYmxlU3RhbmRhcmRzL01QRUctREFTSF9zY2hlbWFfZmlsZXMvREFTSC1NUEQueHNkIiBwcm9maWxlcz0idXJuOm1wZWc6ZGFzaDpwcm9maWxlOmlzb2ZmLWxpdmU6MjAxMSIgdHlwZT0ic3RhdGljIiBtZWRpYVByZXNlbnRhdGlvbkR1cmF0aW9uPSJQVDMwLjIxM1MiIG1heFNlZ21lbnREdXJhdGlvbj0iUFQyLjAwMFMiIG1pbkJ1ZmZlclRpbWU9IlBUNC4xMDZTIj4KICA8QmFzZVVSTD5odHRwczovL3BpbGdyaW0ucXN0di5vbi5lcGljZ2FtZXMuY29tL3VtbXFVcUNTRlVXZVFmelR2cy9mMjUyOGZhMS01ZjMwLTQyZmYtOGFlNS1hMDNlM2IwMjNhMGEvPC9CYXNlVVJMPgogIDxQcm9ncmFtSW5mb3JtYXRpb24+PC9Qcm9ncmFtSW5mb3JtYXRpb24+CiAgPFBlcmlvZCBpZD0iMCIgc3RhcnQ9IlBUMFMiPgogICAgPEFkYXB0YXRpb25TZXQgaWQ9IjAiIGNvbnRlbnRUeXBlPSJhdWRpbyIgc3RhcnRXaXRoU0FQPSIxIiBzZWdtZW50QWxpZ25tZW50PSJ0cnVlIiBiaXRzdHJlYW1Td2l0Y2hpbmc9InRydWUiPgogICAgICA8UmVwcmVzZW50YXRpb24gaWQ9IjAiIGF1ZGlvU2FtcGxpbmdSYXRlPSI0ODAwMCIgYmFuZHdpZHRoPSIxMjgwMDAiIG1pbWVUeXBlPSJhdWRpby9tcDQiIGNvZGVjcz0ibXA0YS40MC4yIj4KICAgICAgICA8U2VnbWVudFRlbXBsYXRlIGR1cmF0aW9uPSIyMDAwMDAwIiB0aW1lc2NhbGU9IjEwMDAwMDAiIGluaXRpYWxpemF0aW9uPSJpbml0XyRSZXByZXNlbnRhdGlvbklEJC5tcDQiIG1lZGlhPSJzZWdtZW50XyRSZXByZXNlbnRhdGlvbklEJF8kTnVtYmVyJC5tNHMiIHN0YXJ0TnVtYmVyPSIxIj48L1NlZ21lbnRUZW1wbGF0ZT4KICAgICAgICA8QXVkaW9DaGFubmVsQ29uZmlndXJhdGlvbiBzY2hlbWVJZFVyaT0idXJuOm1wZWc6ZGFzaDoyMzAwMzozOmF1ZGlvX2NoYW5uZWxfY29uZmlndXJhdGlvbjoyMDExIiB2YWx1ZT0iMiI+PC9BdWRpb0NoYW5uZWxDb25maWd1cmF0aW9uPgogICAgICA8L1JlcHJlc2VudGF0aW9uPgogICAgPC9BZGFwdGF0aW9uU2V0PgogIDwvUGVyaW9kPgo8L01QRD4=",
                ["playlistType"] = "application/dash+xml",
                ["metadata"] = new JsonObject
                {
                    ["assetId"] = "",
                    ["baseUrls"] = new JsonArray { "https://fortnite-public-service-prod11.ol.epicgames.com/ummqUqCSFUWeQfzTvs/f2528fa1-5f30-42ff-8ae5-a03e3b023a0a/" },
                    ["supportsCaching"] = true,
                    ["ucp"] = "a",
                    ["version"] = "f2528fa1-5f30-42ff-8ae5-a03e3b023a0a"
                }
            }));

        app.MapPost("/fortnite/api/feedback/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 200));

        app.MapPost("/fortnite/api/statsv2/query", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));
        app.MapPost("/statsproxy/api/statsv2/query", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapPost("/fortnite/api/game/v2/events/v2/setSubgroup/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));

        app.MapGet("/fortnite/api/game/v2/enabled_features", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapGet("/v1/item/{**rest}", async (HttpContext ctx) =>
        {
            var data = await File.ReadAllBytesAsync(Path.Combine(Paths.PublicDir, "images", "lawinpfp.png"));
            await ResponseUtil.Bytes(ctx, data, "image/png");
        });

        app.MapGet("/api/v1/events/Fortnite/download/{accountId}", async (HttpContext ctx) =>
        {
            var memory = Functions.GetVersionInfo(ctx);
            var tournament = (JsonObject)DataStore.Response("Athena/Tournament/tournament.json").DeepClone();
            ((JsonObject)tournament["player"])["accountId"] = RequestUtil.Route(ctx, "accountId");

            if (memory.Season >= 33)
            {
                var nowMinusOneDay = Functions.Iso(DateTime.UtcNow.AddDays(-1)).Split('T')[0];
                try
                {
                    var eventObj = (JsonObject)((JsonArray)tournament["events"])[0];
                    var window = (JsonObject)((JsonArray)eventObj["eventWindows"])[0];

                    eventObj["beginTime"] = Regex.Replace(eventObj.GetStr("beginTime"), "^[^T]+", nowMinusOneDay);
                    window["beginTime"] = Regex.Replace(window.GetStr("beginTime"), "^[^T]+", nowMinusOneDay);
                    window["countdownBeginTime"] = Regex.Replace(window.GetStr("countdownBeginTime"), "^[^T]+", nowMinusOneDay);
                    eventObj["endTime"] = Regex.Replace(eventObj.GetStr("endTime"), "^[^T]+", nowMinusOneDay);
                    window["endTime"] = Regex.Replace(window.GetStr("endTime"), "^[^T]+", nowMinusOneDay);
                }
                catch { }
            }

            await ResponseUtil.Json(ctx, tournament);
        });

        app.MapGet("/api/v1/events/Fortnite/{eventId}/history/{accountId}", async (HttpContext ctx) =>
        {
            var history = (JsonArray)DataStore.Response("Athena/Tournament/history.json").DeepClone();
            var first = history.Count > 0 ? history[0] as JsonObject : null;
            if (first != null)
            {
                ((JsonObject)first["scoreKey"])["eventId"] = RequestUtil.Route(ctx, "eventId");
                first["teamId"] = RequestUtil.Route(ctx, "accountId");
                first["teamAccountIds"] = new JsonArray { RequestUtil.Route(ctx, "accountId") };
            }
            await ResponseUtil.Json(ctx, history);
        });

        app.MapGet("/api/v1/players/Fortnite/tokens", async (HttpContext ctx) =>
        {
            var teamAccountIds = (RequestUtil.Query(ctx, "teamAccountIds") ?? "").Split(',');
            var tournament = DataStore.Response("Athena/Tournament/tournament.json") as JsonObject;
            var response = new JsonObject { ["accounts"] = new JsonArray() };

            foreach (var accountId in teamAccountIds)
            {
                ((JsonArray)response["accounts"]).Add(new JsonObject
                {
                    ["accountId"] = accountId,
                    ["tokens"] = tournament?["player"]?["tokens"]?.DeepClone()
                });
            }

            await ResponseUtil.Json(ctx, response);
        });

        app.MapGet("/api/v1/leaderboards/Fortnite/{eventId}/{eventWindowId}/{accountId}", async (HttpContext ctx) =>
        {
            var leaderboards = (JsonObject)DataStore.Response("Athena/Tournament/leaderboard.json").DeepClone();
            var heroNames = ((JsonArray)DataStore.Response("Campaign/heroNames.json").DeepClone())
                .Select(n => J.AsString(n)).Where(n => n != null).ToList();
            Shuffle(heroNames);
            heroNames.Insert(0, RequestUtil.Route(ctx, "accountId"));

            var eventId = RequestUtil.Route(ctx, "eventId");
            var eventWindowId = RequestUtil.Route(ctx, "eventWindowId");
            leaderboards["eventId"] = eventId;
            leaderboards["eventWindowId"] = eventWindowId;

            var entryTemplate = leaderboards["entryTemplate"] as JsonObject ?? new JsonObject();
            if (leaderboards["entries"] is not JsonArray entries) entries = new JsonArray();

            for (var i = 0; i < heroNames.Count; i++)
            {
                var entry = (JsonObject)entryTemplate.DeepClone();
                entry["eventId"] = eventId;
                entry["eventWindowId"] = eventWindowId;
                entry["teamAccountIds"] = new JsonArray { heroNames[i] };
                entry["teamId"] = heroNames[i];
                entry["pointsEarned"] = 69 - i;
                entry["score"] = 69 - i;

                var splittedPoints = Random.Shared.Next(0, Math.Max(1, 69 - i));
                entry["pointBreakdown"] = new JsonObject
                {
                    ["PLACEMENT_STAT_INDEX:13"] = new JsonObject { ["timesAchieved"] = 13, ["pointsEarned"] = splittedPoints },
                    ["TEAM_ELIMS_STAT_INDEX:37"] = new JsonObject { ["timesAchieved"] = 13, ["pointsEarned"] = (69 - i) - splittedPoints }
                };
                entry["rank"] = i + 1;
                entries.Add(entry);
            }

            leaderboards["entries"] = entries;
            await ResponseUtil.Json(ctx, leaderboards);
        });

        app.MapGet("/fortnite/api/game/v2/twitch/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 200));

        app.MapGet("/fortnite/api/game/v2/world/info", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, Functions.GetTheater(ctx)));

        app.MapPost("/fortnite/api/game/v2/chat/{a}/{b}/{c}/pc", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["GlobalChatRooms"] = new JsonArray { new JsonObject { ["roomName"] = "lawinserverglobal" } }
            }));

        app.MapPost("/fortnite/api/game/v2/chat/{a}/recommendGeneralChatRooms/pc", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));

        app.MapGet("/presence/api/v1/_/{accountId}/last-online", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));

        app.MapGet("/fortnite/api/receipts/v1/account/{accountId}/receipts", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapGet("/fortnite/api/game/v2/leaderboards/cohort/{accountId}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["accountId"] = RequestUtil.Route(ctx, "accountId"),
                ["cohortAccounts"] = new JsonArray
                {
                    RequestUtil.Route(ctx, "accountId"),
                    "Lawin",
                    "TI93",
                    "PRO100KatYT",
                    "Playeereq",
                    "Matteoki"
                },
                ["expiresAt"] = "9999-12-31T00:00:00.000Z",
                ["playlist"] = RequestUtil.Query(ctx, "playlist")
            }));

        app.MapPost("/fortnite/api/leaderboards/type/group/stat/{statName}/window/{statWindow}", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadArrayAsync(ctx);
            var entries = new JsonArray();
            foreach (var item in body)
            {
                entries.Add(new JsonObject
                {
                    ["accountId"] = J.AsString(item),
                    ["value"] = Random.Shared.Next(1, 69)
                });
            }
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["entries"] = entries,
                ["statName"] = RequestUtil.Route(ctx, "statName"),
                ["statWindow"] = RequestUtil.Route(ctx, "statWindow")
            });
        });

        app.MapPost("/fortnite/api/leaderboards/type/global/stat/{statName}/window/{statWindow}", async (HttpContext ctx) =>
        {
            var heroNames = DataStore.Response("Campaign/heroNames.json") as JsonArray;
            var entries = new JsonArray();
            if (heroNames != null)
            {
                foreach (var name in heroNames)
                {
                    entries.Add(new JsonObject
                    {
                        ["accountId"] = J.AsString(name),
                        ["value"] = Random.Shared.Next(1, 69)
                    });
                }
            }
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["entries"] = entries,
                ["statName"] = RequestUtil.Route(ctx, "statName"),
                ["statWindow"] = RequestUtil.Route(ctx, "statWindow")
            });
        });

        app.MapGet("/fortnite/api/game/v2/homebase/allowed-name-chars", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["ranges"] = new JsonArray
                {
                    48, 57, 65, 90, 97, 122, 192, 255, 260, 265, 280, 281, 286, 287, 304, 305, 321, 324,
                    346, 347, 350, 351, 377, 380, 1024, 1279, 1536, 1791, 4352, 4607, 11904, 12031,
                    12288, 12351, 12352, 12543, 12592, 12687, 12800, 13055, 13056, 13311, 13312, 19903,
                    19968, 40959, 43360, 43391, 44032, 55215, 55216, 55295, 63744, 64255, 65072, 65103,
                    65281, 65470, 131072, 173791, 194560, 195103
                },
                ["singlePoints"] = new JsonArray { 32, 39, 45, 46, 95, 126 },
                ["excludedPoints"] = new JsonArray { 208, 215, 222, 247 }
            }));

        app.MapPost("/datarouter/api/v1/public/data", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));

        app.MapPost("/api/v1/assets/Fortnite/{a}/{b}", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadObjectAsync(ctx);
            if (body.Has("FortCreativeDiscoverySurface") && body.GetLong("FortCreativeDiscoverySurface") == 0)
            {
                await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("Athena/Discovery/discovery_api_assets.json").DeepClone());
            }
            else
            {
                await ResponseUtil.Json(ctx, new JsonObject
                {
                    ["FortCreativeDiscoverySurface"] = new JsonObject
                    {
                        ["meta"] = new JsonObject
                        {
                            ["promotion"] = body.Get("FortCreativeDiscoverySurface")?.DeepClone() ?? 0
                        },
                        ["assets"] = new JsonObject()
                    }
                });
            }
        });

        app.MapGet("/region", async (HttpContext ctx) => await ResponseUtil.Json(ctx, Region()));

        app.MapMethods("/v1/epic-settings/public/users/{**rest}", new[] { "GET", "POST", "PUT", "DELETE", "PATCH" },
            async (HttpContext ctx) => await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("epic-settings.json").DeepClone()));

        app.MapPost("/epic/socs/v1/users/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("socs-settings.json").DeepClone()));

        app.MapGet("/fortnite/api/game/v2/br-inventory/account/{accountId}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["stash"] = new JsonObject { ["globalcash"] = 5000 }
            }));
    }

    private static async Task StatsResponse(HttpContext ctx)
    {
        await ResponseUtil.Json(ctx, new JsonObject
        {
            ["startTime"] = 0,
            ["endTime"] = 0,
            ["stats"] = new JsonObject(),
            ["accountId"] = RequestUtil.Route(ctx, "accountId")
        });
    }

    private static void Shuffle<T>(IList<T> list)
    {
        for (var i = list.Count - 1; i > 0; i--)
        {
            var j = Random.Shared.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private static JsonObject Region() => new()
    {
        ["continent"] = new JsonObject
        {
            ["code"] = "EU",
            ["geoname_id"] = 6255148,
            ["names"] = new JsonObject
            {
                ["de"] = "Europa", ["en"] = "Europe", ["es"] = "Europa", ["fr"] = "Europe",
                ["ja"] = "ヨーロッパ", ["pt-BR"] = "Europa", ["ru"] = "Европа", ["zh-CN"] = "欧洲"
            }
        },
        ["country"] = new JsonObject
        {
            ["geoname_id"] = 2635167,
            ["is_in_european_union"] = false,
            ["iso_code"] = "GB",
            ["names"] = new JsonObject
            {
                ["de"] = "UK", ["en"] = "United Kingdom", ["es"] = "RU", ["fr"] = "Royaume Uni",
                ["ja"] = "英国", ["pt-BR"] = "Reino Unido", ["ru"] = "Британия", ["zh-CN"] = "英国"
            }
        },
        ["subdivisions"] = new JsonArray
        {
            new JsonObject
            {
                ["geoname_id"] = 6269131,
                ["iso_code"] = "ENG",
                ["names"] = new JsonObject
                {
                    ["de"] = "England", ["en"] = "England", ["es"] = "Inglaterra", ["fr"] = "Angleterre",
                    ["ja"] = "イングランド", ["pt-BR"] = "Inglaterra", ["ru"] = "Англия", ["zh-CN"] = "英格兰"
                }
            },
            new JsonObject
            {
                ["geoname_id"] = 3333157,
                ["iso_code"] = "KEC",
                ["names"] = new JsonObject { ["en"] = "Royal Kensington and Chelsea" }
            }
        }
    };
}
