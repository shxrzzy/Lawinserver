using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpPurchaseCatalogHandler
{
    public static async Task PurchaseCatalogEntry(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var campaign = McpCommon.Profile("campaign");
        var athena = McpCommon.Profile("athena");
        var cardPackData = DataStore.Response("Campaign/cardPackData.json") as JsonObject ?? new JsonObject();

        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var purchasedLlama = false;
        var athenaModified = false;
        var itemExists = false;

        var offerId = body.GetStr("offerId");
        var purchaseQuantity = body.GetLong("purchaseQuantity");
        if (purchaseQuantity <= 0) purchaseQuantity = 1;

        if (!string.IsNullOrEmpty(offerId) && string.Equals(profileId, "profile0", StringComparison.OrdinalIgnoreCase) && !purchasedLlama)
        {
            var storefronts = McpCommon.Catalog["storefronts"] as JsonArray;
            if (storefronts != null)
            {
                foreach (var storefrontNode in storefronts)
                {
                    if (storefrontNode is not JsonObject storefront) continue;
                    var storefrontName = storefront.GetStr("name");
                    if (!storefrontName.ToLower().StartsWith("cardpack")) continue;

                    var entries = storefront["catalogEntries"] as JsonArray;
                    if (entries == null) continue;

                    foreach (var entryNode in entries)
                    {
                        if (entryNode is not JsonObject entry) continue;
                        if (entry.GetStr("offerId") != offerId) continue;

                        var grants = entry["itemGrants"] as JsonArray;
                        if (grants != null)
                        {
                            foreach (var grantNode in grants)
                            {
                                var grant = grantNode as JsonObject;
                                var templateId = grant?.GetStr("templateId") ?? J.AsString(grantNode) ?? "";
                                var grantQuantity = grant?.GetLong("quantity") ?? 1;
                                var quantity = purchaseQuantity * grantQuantity;

                                for (var i = 0; i < quantity; i++)
                                {
                                    var id = Functions.MakeId();
                                    var item = new JsonObject
                                    {
                                        ["templateId"] = templateId,
                                        ["attributes"] = new JsonObject
                                        {
                                            ["is_loot_tier_overridden"] = false,
                                            ["max_level_bonus"] = 0,
                                            ["level"] = 1391,
                                            ["pack_source"] = "Schedule",
                                            ["item_seen"] = false,
                                            ["xp"] = 0,
                                            ["favorite"] = false,
                                            ["override_loot_tier"] = 0
                                        },
                                        ["quantity"] = 1
                                    };

                                    McpCommon.Items(profile)[id] = item;
                                    changes.Add(McpCommon.ItemAdded(id, item));

                                    if (entry["prices"] is JsonArray prices && prices.Count > 0)
                                    {
                                        var price = prices[0] as JsonObject;
                                        if (J.AsString(price["currencyType"]).ToLower() == "mtxcurrency")
                                        {
                                            var platform = J.AsString(profile["stats"]?["attributes"]?["current_mtx_platform"]);
                                            var spent = price.GetDouble("finalPrice") * purchaseQuantity;
                                            foreach (var key in McpCommon.Items(profile).Select(kv => kv.Key).ToList())
                                            {
                                                var existing = McpCommon.Item(profile, key);
                                                if (existing == null) continue;
                                                if (!J.AsString(existing["templateId"]).ToLower().StartsWith("currency:mtx")) continue;
                                                var existingPlatform = J.AsString(existing["attributes"]?["platform"]);
                                                if (J.AsString(existingPlatform).ToLower() != J.AsString(platform).ToLower() &&
                                                    existingPlatform != "shared") continue;

                                                existing["quantity"] = existing.GetLong("quantity") - (long)spent;
                                                changes.Add(McpCommon.ItemQuantityChanged(key, existing.GetLong("quantity")));
                                                profile["rvn"] = profile.GetLong("rvn") + 1;
                                                profile["commandRevision"] = profile.GetLong("commandRevision") + 1;
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        if (!string.IsNullOrEmpty(offerId))
        {
            var battlePassChanges = new JsonArray();
            var lootList = new JsonArray();

            void ApplyTierRewards(JsonObject freeTier, JsonObject paidTier, bool isPaid, JsonObject seasonData, JsonObject seasonDataEntry)
            {
                var source = isPaid ? paidTier : freeTier;
                foreach (var tierReward in source)
                {
                    var item = tierReward.Key;
                    var lowered = item.ToLower();
                    var value = J.AsLong(tierReward.Value);

                    if (lowered == "token:athenaseasonxpboost")
                    {
                        var current = seasonDataEntry.GetLong("battlePassXPBoost");
                        seasonDataEntry["battlePassXPBoost"] = current + value;
                        battlePassChanges.Add(McpCommon.StatModified("season_match_boost", seasonDataEntry["battlePassXPBoost"]));
                        lootList.Add(new JsonObject { ["itemType"] = item, ["itemGuid"] = item, ["quantity"] = value });
                    }

                    if (lowered == "token:athenaseasonfriendxpboost")
                    {
                        var current = seasonDataEntry.GetLong("battlePassXPFriendBoost");
                        seasonDataEntry["battlePassXPFriendBoost"] = current + value;
                        battlePassChanges.Add(McpCommon.StatModified("season_friend_match_boost", seasonDataEntry["battlePassXPFriendBoost"]));
                        lootList.Add(new JsonObject { ["itemType"] = item, ["itemGuid"] = item, ["quantity"] = value });
                    }

                    if (lowered.StartsWith("currency:mtx"))
                    {
                        foreach (var key in McpCommon.Items(profile).Select(kv => kv.Key).ToList())
                        {
                            var existing = McpCommon.Item(profile, key);
                            if (existing == null) continue;
                            if (!J.AsString(existing["templateId"]).ToLower().StartsWith("currency:mtx")) continue;
                            if (J.AsString(existing["attributes"]?["platform"]).ToLower() != J.AsString(profile["stats"]?["attributes"]?["current_mtx_platform"]).ToLower() &&
                                J.AsString(existing["attributes"]?["platform"]) != "shared") continue;

                            existing["quantity"] = existing.GetLong("quantity") + value;
                            break;
                        }
                    }

                    if (lowered.StartsWith("homebasebanner"))
                    {
                        foreach (var key in McpCommon.Items(profile).Select(kv => kv.Key).ToList())
                        {
                            var existing = McpCommon.Item(profile, key);
                            if (existing == null) continue;
                            if (J.AsString(existing["templateId"]) != lowered) continue;

                            var attrs = existing["attributes"] as JsonObject;
                            if (attrs != null) attrs["item_seen"] = false;
                            itemExists = true;
                            changes.Add(McpCommon.ItemAttrChanged(key, "item_seen", attrs?["item_seen"]));
                        }

                        if (!itemExists)
                        {
                            var itemId = Functions.MakeId();
                            var newItem = new JsonObject
                            {
                                ["templateId"] = lowered,
                                ["attributes"] = new JsonObject { ["item_seen"] = false },
                                ["quantity"] = 1
                            };
                            McpCommon.Items(profile)[itemId] = newItem;
                            changes.Add(McpCommon.ItemAdded(itemId, newItem));
                        }

                        itemExists = false;
                    }

                    if (lowered.StartsWith("athena"))
                    {
                        foreach (var key in McpCommon.Items(athena).Select(kv => kv.Key).ToList())
                        {
                            var existing = McpCommon.Item(athena, key);
                            if (existing == null) continue;
                            if (J.AsString(existing["templateId"]) != lowered) continue;

                            var attrs = existing["attributes"] as JsonObject;
                            if (attrs != null) attrs["item_seen"] = false;
                            itemExists = true;
                            battlePassChanges.Add(McpCommon.ItemAttrChanged(key, "item_seen", attrs?["item_seen"]));
                        }

                        if (!itemExists)
                        {
                            var itemId = Functions.MakeId();
                            var newItem = new JsonObject
                            {
                                ["templateId"] = lowered,
                                ["attributes"] = new JsonObject
                                {
                                    ["max_level_bonus"] = 0,
                                    ["level"] = 1,
                                    ["item_seen"] = false,
                                    ["xp"] = 0,
                                    ["variants"] = new JsonArray(),
                                    ["favorite"] = false
                                },
                                ["quantity"] = isPaid ? value : (value > 0 ? value : 1)
                            };
                            McpCommon.Items(athena)[itemId] = newItem;
                            battlePassChanges.Add(McpCommon.ItemAdded(itemId, newItem));
                        }

                        itemExists = false;
                    }

                    lootList.Add(new JsonObject { ["itemType"] = item, ["itemGuid"] = item, ["quantity"] = value });
                }
            }

            var storefronts = McpCommon.Catalog["storefronts"] as JsonArray;
            if (storefronts != null)
            {
                foreach (var storefrontNode in storefronts)
                {
                    if (storefrontNode is not JsonObject storefront) continue;
                    var storefrontName = storefront.GetStr("name");
                    if (!storefrontName.StartsWith("BRSeason")) continue;
                    if (!long.TryParse(storefrontName.Substring("BRSeason".Length), out var seasonNumber)) continue;

                    var entries = storefront["catalogEntries"] as JsonArray;
                    if (entries == null) continue;

                    foreach (var entryNode in entries)
                    {
                        if (entryNode is not JsonObject entry) continue;
                        if (entry.GetStr("offerId") != offerId) continue;

                        if (multiUpdate.Count == 0)
                        {
                            multiUpdate.Add(new JsonObject
                            {
                                ["profileRevision"] = athena.GetLong("rvn"),
                                ["profileId"] = "athena",
                                ["profileChangesBaseRevision"] = athena.GetLong("rvn"),
                                ["profileChanges"] = new JsonArray(),
                                ["profileCommandRevision"] = athena.GetLong("commandRevision")
                            });
                        }

                        var seasonKey = "Season" + seasonNumber;
                        var battlePass = DataStore.Response("Athena/BattlePass/" + seasonKey + ".json") as JsonObject;
                        if (battlePass == null) break;

                        var seasonData = DataStore.Response("Athena/SeasonData.json") as JsonObject;
                        if (seasonData?[seasonKey] is not JsonObject seasonDataEntry) break;

                        long endingTier = seasonDataEntry.GetLong("battlePassTier");
                        seasonDataEntry["battlePassPurchased"] = true;

                        if (battlePass.GetStr("battleBundleOfferId") == offerId)
                        {
                            endingTier += 25;
                            if (endingTier > 100) endingTier = 100;
                        }

                        for (var i = 0; i < endingTier; i++)
                        {
                            var freeRewards = battlePass["freeRewards"] as JsonArray;
                            var paidRewards = battlePass["paidRewards"] as JsonArray;
                            var freeTier = freeRewards != null && i < freeRewards.Count ? freeRewards[i] as JsonObject ?? new JsonObject() : new JsonObject();
                            var paidTier = paidRewards != null && i < paidRewards.Count ? paidRewards[i] as JsonObject ?? new JsonObject() : new JsonObject();
                            ApplyTierRewards(freeTier, paidTier, false, seasonData, seasonDataEntry);
                            ApplyTierRewards(freeTier, paidTier, true, seasonData, seasonDataEntry);
                        }

                        var giftBoxId = Functions.MakeId();
                        var giftBox = new JsonObject
                        {
                            ["templateId"] = seasonNumber <= 4 ? "GiftBox:gb_battlepass" : "GiftBox:gb_battlepasspurchased",
                            ["attributes"] = new JsonObject { ["max_level_bonus"] = 0, ["fromAccountId"] = "", ["lootList"] = lootList }
                        };

                        if (seasonNumber > 2)
                        {
                            McpCommon.Items(profile)[giftBoxId] = giftBox;
                            changes.Add(McpCommon.ItemAdded(giftBoxId, giftBox));
                        }

                        if (multiUpdate.Count > 0)
                        {
                            battlePassChanges.Add(McpCommon.StatModified("book_purchased", seasonDataEntry["battlePassPurchased"]));
                            battlePassChanges.Add(McpCommon.StatModified("book_level", seasonDataEntry["battlePassTier"]));
                            var mu = multiUpdate[0] as JsonObject;
                            if (mu != null) mu["profileChanges"] = battlePassChanges;
                        }

                        athenaModified = true;
                        DataStore.SaveResponse("Athena/SeasonData.json", seasonData);
                    }

                    if (athenaModified) break;
                }
            }
        }

        if (athenaModified)
        {
            athena["rvn"] = athena.GetLong("rvn") + 1;
            athena["commandRevision"] = athena.GetLong("commandRevision") + 1;

            if (multiUpdate.Count > 0)
            {
                var mu = multiUpdate[0] as JsonObject;
                if (mu != null)
                {
                    mu["profileRevision"] = athena.GetLong("rvn");
                    mu["profileCommandRevision"] = athena.GetLong("commandRevision");
                }
            }

            McpCommon.Save("athena", athena);
            McpCommon.Save(profileId, profile);
        }
        else
        {
            campaign["rvn"] = campaign.GetLong("rvn") + 1;
            campaign["commandRevision"] = campaign.GetLong("commandRevision") + 1;

            if (multiUpdate.Count > 0)
            {
                var mu = multiUpdate[0] as JsonObject;
                if (mu != null)
                {
                    mu["profileRevision"] = campaign.GetLong("rvn");
                    mu["profileCommandRevision"] = campaign.GetLong("commandRevision");
                }
            }

            McpCommon.Save("campaign", campaign);
            McpCommon.Save(profileId, profile);
        }

        
        if (McpCommon.QueryRevision(ctx) != baseRevision)
            changes = new JsonArray { McpCommon.FullUpdate(profile) };

        var response = new JsonObject
        {
            ["profileRevision"] = profile.GetLong("rvn"),
            ["profileId"] = profileId,
            ["profileChangesBaseRevision"] = baseRevision,
            ["profileChanges"] = changes,
            ["notifications"] = notifications,
            ["profileCommandRevision"] = profile.GetLong("commandRevision"),
            ["serverTime"] = Functions.IsoNow(),
            ["multiUpdate"] = multiUpdate,
            ["responseVersion"] = 1
        };

        await ResponseUtil.Json(ctx, response);
    }
}




