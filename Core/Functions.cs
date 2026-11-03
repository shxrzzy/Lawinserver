using System.Globalization;
using System.Text;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LawinServer.WebSockets;

namespace LawinServer.Core;

public static class Functions
{
    private static readonly Random Rng = new();

    public static string Iso(DateTime dt) =>
        dt.ToUniversalTime().ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", CultureInfo.InvariantCulture);

    public static string IsoNow() => Iso(DateTime.UtcNow);

    public static string MakeId() => Guid.NewGuid().ToString();

    public static string DecodeBase64(string str) => Encoding.UTF8.GetString(Convert.FromBase64String(str));

    public static VersionInfo GetVersionInfo(HttpContext ctx)
    {
        var memory = new VersionInfo { Season = 0, Build = 0.0, CL = "", Lobby = "" };
        var ua = RequestUtil.UserAgent(ctx);
        if (string.IsNullOrEmpty(ua)) return memory;

        var cl = "";
        try
        {
            var split = ua.Split('-');
            var buildId = split.Length > 3 ? split[3].Split(',')[0] : "";
            if (IsNumeric(buildId)) cl = buildId;
            if (!IsNumeric(buildId))
            {
                buildId = split.Length > 3 ? split[3].Split(' ')[0] : "";
                if (IsNumeric(buildId)) cl = buildId;
            }
        }
        catch
        {
            try
            {
                var split = ua.Split('-');
                var buildId = split.Length > 1 ? split[1].Split('+')[0] : "";
                if (IsNumeric(buildId)) cl = buildId;
            }
            catch { }
        }

        try
        {
            var release = ua.Split("Release-");
            var build = release.Length > 1 ? release[1].Split('-')[0] : "";
            if (build.Split('.').Length == 3)
            {
                var value = build.Split('.');
                build = value[0] + "." + value[1] + value[2];
            }

            var season = (int)double.Parse(build.Split('.')[0], CultureInfo.InvariantCulture);
            memory.Season = season;
            memory.Build = double.Parse(build, CultureInfo.InvariantCulture);
            memory.CL = cl;
            memory.Lobby = $"LobbySeason{memory.Season}";
        }
        catch
        {
            memory.Season = 2;
            memory.Build = 2.0;
            memory.CL = cl;
            memory.Lobby = "LobbyWinterDecor";
        }

        return memory;
    }

    private static bool IsNumeric(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        return double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    
    
    

    public static JsonObject GetItemShop()
    {
        var catalog = (JsonObject)DataStore.Response("catalog.json").DeepClone();
        var catalogConfig = DataStore.CatalogConfig() as JsonObject;
        if (catalogConfig == null) return catalog;

        try
        {
            foreach (var pair in catalogConfig)
            {
                var value = pair.Key;
                var cfg = pair.Value as JsonObject;
                if (cfg == null) continue;

                var grants = cfg["itemGrants"] as JsonArray;
                if (grants == null || grants.Count == 0) continue;

                var entry = NewCatalogEntry();

                if (value.StartsWith("daily", StringComparison.OrdinalIgnoreCase))
                {
                    var storefronts = catalog["storefronts"] as JsonArray;
                    if (storefronts != null)
                    {
                        foreach (var sfNode in storefronts)
                        {
                            if (sfNode is not JsonObject storefront) continue;
                            if (storefront.GetStr("name") != "BRDailyStorefront") continue;

                            entry["requirements"] = new JsonArray();
                            entry["itemGrants"] = new JsonArray();

                            foreach (var grantNode in grants)
                            {
                                var grant = J.AsString(grantNode);
                                if (string.IsNullOrEmpty(grant)) continue;

                                var first = J.AsString(grants[0]);
                                entry["devName"] = first;
                                entry["offerId"] = first;

                                ((JsonArray)entry["requirements"]).Add(new JsonObject
                                {
                                    ["requirementType"] = "DenyOnItemOwnership",
                                    ["requiredId"] = grant,
                                    ["minQuantity"] = 1
                                });
                                ((JsonArray)entry["itemGrants"]).Add(new JsonObject
                                {
                                    ["templateId"] = grant,
                                    ["quantity"] = 1
                                });
                                ((JsonObject)entry["meta"])["templateId"] = grant;
                                ((JsonArray)entry["metaInfo"]).Add(new JsonObject
                                {
                                    ["key"] = "templateId",
                                    ["value"] = grant
                                });
                            }

                            var price = cfg.GetLong("price");
                            var priceObj = (JsonObject)((JsonArray)entry["prices"])[0];
                            priceObj["basePrice"] = price;
                            priceObj["regularPrice"] = price;
                            priceObj["finalPrice"] = price;

                            entry["sortPriority"] = -1;

                            if (((JsonArray)entry["itemGrants"]).Count != 0)
                                ((JsonArray)storefront["catalogEntries"]).Add(entry);
                        }
                    }
                }

                if (value.StartsWith("featured", StringComparison.OrdinalIgnoreCase))
                {
                    var storefronts = catalog["storefronts"] as JsonArray;
                    if (storefronts != null)
                    {
                        foreach (var sfNode in storefronts)
                        {
                            if (sfNode is not JsonObject storefront) continue;
                            if (storefront.GetStr("name") != "BRWeeklyStorefront") continue;

                            entry["requirements"] = new JsonArray();
                            entry["itemGrants"] = new JsonArray();

                            foreach (var grantNode in grants)
                            {
                                var grant = J.AsString(grantNode);
                                if (string.IsNullOrEmpty(grant)) continue;

                                var first = J.AsString(grants[0]);
                                entry["devName"] = first;
                                entry["offerId"] = first;

                                ((JsonArray)entry["requirements"]).Add(new JsonObject
                                {
                                    ["requirementType"] = "DenyOnItemOwnership",
                                    ["requiredId"] = grant,
                                    ["minQuantity"] = 1
                                });
                                ((JsonArray)entry["itemGrants"]).Add(new JsonObject
                                {
                                    ["templateId"] = grant,
                                    ["quantity"] = 1
                                });
                                ((JsonObject)entry["meta"])["templateId"] = grant;
                                ((JsonArray)entry["metaInfo"]).Add(new JsonObject
                                {
                                    ["key"] = "templateId",
                                    ["value"] = grant
                                });
                            }

                            var price = cfg.GetLong("price");
                            var priceObj = (JsonObject)((JsonArray)entry["prices"])[0];
                            priceObj["basePrice"] = price;
                            priceObj["regularPrice"] = price;
                            priceObj["finalPrice"] = price;

                            ((JsonObject)entry["meta"])["TileSize"] = "Normal";
                            if (((JsonArray)entry["metaInfo"]).Count > 3)
                                ((JsonObject)((JsonArray)entry["metaInfo"])[3])["value"] = "Normal";

                            if (((JsonArray)entry["itemGrants"]).Count != 0)
                                ((JsonArray)storefront["catalogEntries"]).Add(entry);
                        }
                    }
                }

                var displayAssetPath = cfg.GetStr("displayAssetPath");
                if (!string.IsNullOrEmpty(displayAssetPath))
                {
                    ((JsonObject)entry["meta"])["displayAssetPath"] = displayAssetPath;
                    ((JsonArray)entry["metaInfo"]).Add(new JsonObject
                    {
                        ["key"] = "displayAssetPath",
                        ["value"] = displayAssetPath
                    });
                }

                var newDisplayAssetPath = cfg.GetStr("NewDisplayAssetPath");
                if (!string.IsNullOrEmpty(newDisplayAssetPath))
                {
                    ((JsonObject)entry["meta"])["NewDisplayAssetPath"] = newDisplayAssetPath;
                    ((JsonArray)entry["metaInfo"]).Add(new JsonObject
                    {
                        ["key"] = "NewDisplayAssetPath",
                        ["value"] = newDisplayAssetPath
                    });
                }
            }
        }
        catch
        {
            
        }

        return catalog;
    }

    private static JsonObject NewCatalogEntry()
    {
        return new JsonObject
        {
            ["devName"] = "",
            ["offerId"] = "",
            ["fulfillmentIds"] = new JsonArray(),
            ["dailyLimit"] = -1,
            ["weeklyLimit"] = -1,
            ["monthlyLimit"] = -1,
            ["categories"] = new JsonArray(),
            ["prices"] = new JsonArray
            {
                new JsonObject
                {
                    ["currencyType"] = "MtxCurrency",
                    ["currencySubType"] = "",
                    ["regularPrice"] = 0,
                    ["finalPrice"] = 0,
                    ["saleExpiration"] = "9999-12-02T01:12:00Z",
                    ["basePrice"] = 0
                }
            },
            ["meta"] = new JsonObject
            {
                ["SectionId"] = "Featured",
                ["LayoutId"] = "LawinServer.99",
                ["TileSize"] = "Small",
                ["AnalyticOfferGroupId"] = "LawinServer/Attitude8",
                ["FirstSeen"] = "2/2/2020",
                ["inDate"] = "2018-04-30T00:00:00.000Z",
                ["outDate"] = "9999-12-31T23:59:59.999Z",
                ["color1"] = "#50C878",
                ["color2"] = "#1B5E20",
                ["textBackgroundColor"] = "#0D3D0D"
            },
            ["matchFilter"] = "",
            ["filterWeight"] = 0,
            ["appStoreId"] = new JsonArray(),
            ["requirements"] = new JsonArray(),
            ["offerType"] = "StaticPrice",
            ["giftInfo"] = new JsonObject
            {
                ["bIsEnabled"] = false,
                ["forcedGiftBoxTemplateId"] = "",
                ["purchaseRequirements"] = new JsonArray(),
                ["giftRecordIds"] = new JsonArray()
            },
            ["refundable"] = true,
            ["metaInfo"] = new JsonArray
            {
                new JsonObject { ["key"] = "SectionId", ["value"] = "Featured" },
                new JsonObject { ["key"] = "LayoutId", ["value"] = "LawinServer.99" },
                new JsonObject { ["key"] = "TileSize", ["value"] = "Small" },
                new JsonObject { ["key"] = "AnalyticOfferGroupId", ["value"] = "LawinServer/Attitude8" },
                new JsonObject { ["key"] = "FirstSeen", ["value"] = "2/2/2020" },
                new JsonObject { ["key"] = "inDate", ["value"] = "2018-04-30T00:00:00.000Z" },
                new JsonObject { ["key"] = "outDate", ["value"] = "9999-12-31T23:59:59.999Z" },
                new JsonObject { ["key"] = "color1", ["value"] = "#50C878" },
                new JsonObject { ["key"] = "color2", ["value"] = "#1B5E20" },
                new JsonObject { ["key"] = "textBackgroundColor", ["value"] = "#0D3D0D" }
            },
            ["displayAssetPath"] = "",
            ["itemGrants"] = new JsonArray(),
            ["sortPriority"] = 0,
            ["catalogGroupPriority"] = 0
        };
    }

    
    
    

    public static JsonObject GetTheater(HttpContext ctx)
    {
        var memory = GetVersionInfo(ctx);
        var theater = DataStore.Response("Campaign/worldstw.json")?.ToJsonString(J.Compact) ?? "{}";
        var season = "Season" + memory.Season;

        try
        {
            if (memory.Build >= 30.20)
            {
                theater = Regex.Replace(theater, "/Game/World/ZoneThemes", "/STW_Zones/World/ZoneThemes", RegexOptions.IgnoreCase);
                theater = Regex.Replace(theater, "\"DataTable'/Game/", "\"/Script/Engine.DataTable'/Game/");
            }

            if (memory.Build >= 15.30)
            {
                theater = Regex.Replace(theater, "/Game/", "/SaveTheWorld/");
                theater = Regex.Replace(theater, "\"DataTable'/SaveTheWorld/", "\"DataTable'/Game/");
            }

            var date = DateTime.Now;
            var hour = date.Hour;

            if (memory.Season >= 9)
            {
                date = date.Date.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
            }
            else
            {
                if (hour < 6) date = date.Date.AddHours(5).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                else if (hour < 12) date = date.Date.AddHours(11).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                else if (hour < 18) date = date.Date.AddHours(17).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
                else date = date.Date.AddHours(23).AddMinutes(59).AddSeconds(59).AddMilliseconds(999);
            }

            theater = Regex.Replace(theater, "2017-07-25T23:59:59.999Z", Iso(date));
        }
        catch { }

        var obj = J.Parse(theater) as JsonObject ?? new JsonObject();

        if (obj.ContainsKey("Seasonal") && obj["Seasonal"] is JsonObject seasonal &&
            seasonal.ContainsKey(season) && seasonal[season] is JsonObject seasonalData)
        {
            Concat(obj, "theaters", seasonalData, "theaters");
            Concat(obj, "missions", seasonalData, "missions");
            Concat(obj, "missionAlerts", seasonalData, "missionAlerts");
            obj.Remove("Seasonal");
        }

        return obj;
    }

    private static void Concat(JsonObject target, string targetKey, JsonObject source, string sourceKey)
    {
        if (target[targetKey] is not JsonArray targetArr) return;
        if (source[sourceKey] is not JsonArray sourceArr) return;
        foreach (var item in sourceArr.ToList())
        {
            sourceArr.Remove(item);
            targetArr.Add(item);
        }
    }

    
    
    

    public static void ChooseTranslationsInJson(JsonNode obj, HttpContext ctx, string targetLanguage = "")
    {
        if (string.IsNullOrEmpty(targetLanguage))
        {
            var acceptLanguage = RequestUtil.AcceptLanguage(ctx);
            if (!string.IsNullOrEmpty(acceptLanguage))
            {
                if (acceptLanguage.Contains('-') && acceptLanguage != "es-419" && acceptLanguage != "pt-BR")
                    targetLanguage = acceptLanguage.Split('-')[0];
                else
                    targetLanguage = acceptLanguage;
            }
            else
            {
                targetLanguage = "en";
            }
        }

        ChooseTranslations(obj, targetLanguage);
    }

    private static void ChooseTranslations(JsonNode obj, string targetLanguage)
    {
        if (obj is JsonArray arr)
        {
            foreach (var item in arr)
                ChooseTranslations(item, targetLanguage);
            return;
        }

        if (obj is not JsonObject o) return;

        foreach (var key in o.Select(kv => kv.Key).ToList())
        {
            var value = o[key];
            if (value is JsonObject child)
            {
                if (child.ContainsKey(targetLanguage) || child.ContainsKey("en"))
                {
                    var chosen = child[targetLanguage] ?? child["en"];
                    o[key] = chosen?.DeepClone();
                }
                else
                {
                    ChooseTranslations(child, targetLanguage);
                }
            }
            else if (value is JsonArray childArray)
            {
                ChooseTranslations(childArray, targetLanguage);
            }
        }
    }

    public static JsonObject GetContentPages(HttpContext ctx)
    {
        var memory = GetVersionInfo(ctx);
        var contentpages = (JsonObject)DataStore.Response("contentpages.json").DeepClone();
        ChooseTranslationsInJson(contentpages, ctx);

        var news = new[] { "savetheworldnews", "battleroyalenews" };
        try
        {
            if (memory.Build < 5.30)
            {
                foreach (var mode in news)
                {
                    var messages = contentpages[mode]?["news"]?["messages"] as JsonArray;
                    if (messages == null) continue;
                    if (messages.Count > 0 && messages[0] is JsonObject m0)
                        m0["image"] = "https://fortnite-public-service-prod11.ol.epicgames.com/images/discord-s.png";
                    if (messages.Count > 1 && messages[1] is JsonObject m1)
                        m1["image"] = "https://fortnite-public-service-prod11.ol.epicgames.com/images/lawin-s.png";
                }
            }
        }
        catch { }

        try
        {
            var backgrounds = contentpages["dynamicbackgrounds"]?["backgrounds"]?["backgrounds"] as JsonArray;
            if (backgrounds != null && backgrounds.Count >= 2)
            {
                var season = $"season{memory.Season}{(memory.Season >= 21 ? "00" : "")}";
                ((JsonObject)backgrounds[0])["stage"] = season;
                ((JsonObject)backgrounds[1])["stage"] = season;

                switch (memory.Season)
                {
                    case 9:
                        ((JsonObject)contentpages["lobby"])["backgroundimage"] = "";
                        ((JsonObject)contentpages["lobby"])["stage"] = "default";
                        break;
                    case 10:
                        ((JsonObject)backgrounds[0])["stage"] = "seasonx";
                        ((JsonObject)backgrounds[1])["stage"] = "seasonx";
                        break;
                    case 11: case 12: case 13: case 14: case 15: case 16: case 17: case 18: case 19:
                        break;
                    case 20:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp20-lobby-2048x1024-d89eb522746c.png";
                        break;
                    case 21:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/s21-lobby-background-2048x1024-2e7112b25dc3.jpg";
                        break;
                    case 22:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp22-lobby-square-2048x2048-2048x2048-e4e90c6e8018.jpg";
                        break;
                    case 23:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp23-lobby-2048x1024-2048x1024-26f2c1b27f63.png";
                        break;
                    case 24:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-ch4s2-bp-lobby-4096x2048-edde08d15f7e.jpg";
                        break;
                    case 25:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/s25-lobby-4k-4096x2048-4a832928e11f.jpg";
                        ((JsonObject)backgrounds[1])["backgroundimage"] = "https://cdn2.unrealengine.com/fn-shop-ch4s3-04-1920x1080-785ce1d90213.png";
                        break;
                    case 27:
                        ((JsonObject)backgrounds[0])["stage"] = "rufus";
                        break;
                    default:
                        ((JsonObject)backgrounds[0])["stage"] = "defaultnotris";
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://fortnite-public-service-prod11.ol.epicgames.com/images/lightlobbybg.png";
                        break;
                }

                switch (memory.Build)
                {
                    case 9.30:
                        ((JsonObject)contentpages["lobby"])["stage"] = "summer";
                        break;
                    case 11.10:
                        ((JsonObject)backgrounds[0])["stage"] = "fortnitemares";
                        ((JsonObject)backgrounds[1])["stage"] = "fortnitemares";
                        break;
                    case 11.31:
                    case 11.40:
                        ((JsonObject)backgrounds[0])["stage"] = "winter19";
                        ((JsonObject)backgrounds[1])["stage"] = "winter19";
                        break;
                    case 19.01:
                        ((JsonObject)backgrounds[0])["stage"] = "winter2021";
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp19-lobby-xmas-2048x1024-f85d2684b4af.png";
                        ((JsonObject)((JsonObject)contentpages["subgameinfo"])["battleroyale"])["image"] = "https://cdn2.unrealengine.com/19br-wf-subgame-select-512x1024-16d8bb0f218f.jpg";
                        ((JsonObject)contentpages["specialoffervideo"])["bSpecialOfferEnabled"] = "true";
                        break;
                    case 20.40:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp20-40-armadillo-glowup-lobby-2048x2048-2048x2048-3b83b887cc7f.jpg";
                        break;
                    case 21.10:
                        ((JsonObject)backgrounds[0])["stage"] = "season2100";
                        break;
                    case 21.30:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/nss-lobbybackground-2048x1024-f74a14565061.jpg";
                        ((JsonObject)backgrounds[0])["stage"] = "season2130";
                        break;
                    case 23.10:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-bp23-winterfest-lobby-square-2048x2048-2048x2048-277a476e5ca6.png";
                        ((JsonObject)contentpages["specialoffervideo"])["bSpecialOfferEnabled"] = "true";
                        break;
                    case 25.11:
                        ((JsonObject)backgrounds[0])["backgroundimage"] = "https://cdn2.unrealengine.com/t-s25-14dos-lobby-4096x2048-2be24969eee3.jpg";
                        break;
                }
            }
        }
        catch { }

        return contentpages;
    }

    
    
    

    public static JsonObject MakeSurvivorAttributes(string templateId)
    {
        var survivorData = DataStore.Response("Campaign/survivorData.json") as JsonObject;
        var attributes = new JsonObject
        {
            ["level"] = 1,
            ["item_seen"] = false,
            ["squad_slot_idx"] = -1,
            ["building_slot_used"] = -1
        };

        if (survivorData?["fixedAttributes"] is JsonObject fixedAttributes &&
            fixedAttributes[templateId] is JsonObject fixedAttrs)
        {
            foreach (var kv in fixedAttrs)
                attributes[kv.Key] = kv.Value?.DeepClone();
        }

        if (!attributes.ContainsKey("gender"))
            attributes["gender"] = Random.Shared.Next(1, 3).ToString();

        if (!attributes.ContainsKey("managerSynergy"))
        {
            var bonuses = survivorData?["bonuses"] as JsonArray;
            if (bonuses != null && bonuses.Count > 0)
                attributes["set_bonus"] = bonuses[Random.Shared.Next(bonuses.Count)]?.DeepClone();
        }

        if (!attributes.ContainsKey("personality"))
        {
            var personalities = survivorData?["personalities"] as JsonArray;
            if (personalities != null && personalities.Count > 0)
                attributes["personality"] = personalities[Random.Shared.Next(personalities.Count)]?.DeepClone();
        }

        if (!attributes.ContainsKey("portrait"))
        {
            var portraitFactor = attributes.GetStr("personality");
            if (attributes.ContainsKey("managerSynergy"))
                portraitFactor = attributes.GetStr("managerSynergy");

            var gender = attributes.GetStr("gender");
            var portraits = survivorData?["portraits"]?[portraitFactor]?[gender] as JsonArray;
            if (portraits != null && portraits.Count > 0)
                attributes["portrait"] = portraits[Random.Shared.Next(portraits.Count)]?.DeepClone();
        }

        return attributes;
    }

    public static void SendXmppMessageToAll(JsonNode body)
    {
        var text = body is JsonValue ? J.AsString(body) : body?.ToJsonString(J.Compact) ?? "null";
        XmppServer.Broadcast(text);
    }
}
