using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpWinterfestHandler
{
    public static async Task UnlockRewardNode(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var commonCore = McpCommon.Profile("common_core");
        var winterFest = DataStore.Response("Athena/winterfestRewards.json");
        var memory = Functions.GetVersionInfo(ctx);

        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var statChanged = false;
        var commonCoreChanged = false;
        var itemExists = false;
        var season = $"Season{memory.Season}";

        var giftId = Functions.MakeId();
        var items = McpCommon.Items(profile);
        items[giftId] = McpCommon.NewItem("GiftBox:gb_winterfestreward", new JsonObject
        {
            ["max_level_bonus"] = 0,
            ["fromAccountId"] = "",
            ["lootList"] = new JsonArray(),
            ["level"] = 1,
            ["item_seen"] = false,
            ["xp"] = 0,
            ["giftedOn"] = Functions.IsoNow(),
            ["params"] = new JsonObject
            {
                ["SubGame"] = "Athena",
                ["winterfestGift"] = "true"
            },
            ["favorite"] = false
        });

        var nodeId = body.GetStr("nodeId");
        var rewardGraphId = body.GetStr("rewardGraphId");
        var giftLootList = (JsonArray)((JsonObject)items[giftId]!)["attributes"]!["lootList"]!;

        JsonObject commonCoreUpdate = null;

        if (!string.IsNullOrEmpty(nodeId) && !string.IsNullOrEmpty(rewardGraphId))
        {
            var rewards = winterFest?[season]?[nodeId] as JsonArray;
            if (rewards != null)
            {
                foreach (var rewardNode in rewards)
                {
                    var reward = J.AsString(rewardNode) ?? "";
                    var id = Functions.MakeId();
                    var lowerReward = reward.ToLowerInvariant();

                    if (lowerReward.StartsWith("homebasebannericon:"))
                    {
                        if (!commonCoreChanged)
                        {
                            commonCoreUpdate = new JsonObject
                            {
                                ["profileRevision"] = commonCore.GetLong("rvn"),
                                ["profileId"] = "common_core",
                                ["profileChangesBaseRevision"] = commonCore.GetLong("rvn"),
                                ["profileChanges"] = new JsonArray(),
                                ["profileCommandRevision"] = commonCore.GetLong("commandRevision")
                            };
                            multiUpdate.Add(commonCoreUpdate);
                            commonCoreChanged = true;
                        }

                        var commonItems = McpCommon.Items(commonCore);
                        foreach (var key in commonItems.Select(kv => kv.Key).ToList())
                        {
                            var entry = commonItems[key] as JsonObject;
                            if ((entry?.GetStr("templateId") ?? "").ToLowerInvariant() != lowerReward) continue;

                            if (entry?["attributes"] is JsonObject entryAttrs)
                            {
                                entryAttrs["item_seen"] = false;
                                id = key;
                                itemExists = true;

                                ((JsonArray)commonCoreUpdate["profileChanges"]).Add(McpCommon.ItemAttrChanged(key, "item_seen", false));
                            }
                        }

                        if (!itemExists)
                        {
                            commonItems[id] = NewWinterfestItem(reward);
                            ((JsonArray)commonCoreUpdate["profileChanges"]).Add(McpCommon.ItemAdded(id, commonItems[id]));
                        }

                        itemExists = false;

                        commonCore["rvn"] = commonCore.GetLong("rvn") + 1;
                        commonCore["commandRevision"] = commonCore.GetLong("commandRevision") + 1;

                        commonCoreUpdate["profileRevision"] = commonCore.GetLong("rvn");
                        commonCoreUpdate["profileCommandRevision"] = commonCore.GetLong("commandRevision");

                        giftLootList.Add(new JsonObject
                        {
                            ["itemType"] = reward,
                            ["itemGuid"] = id,
                            ["itemProfile"] = "common_core",
                            ["attributes"] = new JsonObject { ["creation_time"] = Functions.IsoNow() },
                            ["quantity"] = 1
                        });
                    }
                    else
                    {
                        foreach (var key in items.Select(kv => kv.Key).ToList())
                        {
                            var entry = items[key] as JsonObject;
                            if ((entry?.GetStr("templateId") ?? "").ToLowerInvariant() != lowerReward) continue;

                            if (entry?["attributes"] is JsonObject entryAttrs)
                            {
                                entryAttrs["item_seen"] = false;
                                id = key;
                                itemExists = true;

                                changes.Add(McpCommon.ItemAttrChanged(key, "item_seen", false));
                            }
                        }

                        if (!itemExists)
                        {
                            items[id] = NewWinterfestItem(reward);
                            changes.Add(McpCommon.ItemAdded(id, items[id]));
                        }

                        itemExists = false;

                        giftLootList.Add(new JsonObject
                        {
                            ["itemType"] = reward,
                            ["itemGuid"] = id,
                            ["itemProfile"] = "athena",
                            ["attributes"] = new JsonObject { ["creation_time"] = Functions.IsoNow() },
                            ["quantity"] = 1
                        });
                    }
                }

                if (items[rewardGraphId] is JsonObject graphItem && graphItem["attributes"] is JsonObject graphAttrs)
                {
                    var rewardKeys = graphAttrs.GetArr("reward_keys");
                    if (rewardKeys is { Count: > 0 } && rewardKeys[0] is JsonObject rewardKey)
                        rewardKey["unlock_keys_used"] = rewardKey.GetLong("unlock_keys_used") + 1;

                    if (graphAttrs["reward_nodes_claimed"] is JsonArray claimed)
                        claimed.Add(nodeId);
                }

                statChanged = true;
            }
        }

        if (statChanged)
        {
            profile["rvn"] = profile.GetLong("rvn") + 1;
            profile["commandRevision"] = profile.GetLong("commandRevision") + 1;

            changes.Add(McpCommon.ItemAdded(giftId, items[giftId]));

            var graphAttrs = (items[rewardGraphId] as JsonObject)?["attributes"];
            changes.Add(McpCommon.ItemAttrChanged(rewardGraphId, "reward_keys", graphAttrs?["reward_keys"]));
            changes.Add(McpCommon.ItemAttrChanged(rewardGraphId, "reward_nodes_claimed", graphAttrs?["reward_nodes_claimed"]));

            if (memory.Season == 11 && items["S11_GIFT_KEY"] is JsonObject giftKey)
            {
                giftKey["quantity"] = giftKey.GetLong("quantity") - 1;
                changes.Add(McpCommon.ItemQuantityChanged("S11_GIFT_KEY", giftKey.GetLong("quantity")));
            }

            McpCommon.Save(profileId, profile);
            if (commonCoreChanged)
                McpCommon.Save("common_core", commonCore);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, multiUpdate: multiUpdate);
    }

    private static JsonObject NewWinterfestItem(string templateId) => McpCommon.NewItem(templateId, new JsonObject
    {
        ["max_level_bonus"] = 0,
        ["level"] = 1,
        ["item_seen"] = false,
        ["xp"] = 0,
        ["variants"] = new JsonArray(),
        ["favorite"] = false
    });
}
