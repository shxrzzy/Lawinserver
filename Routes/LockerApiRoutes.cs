using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class LockerApiRoutes
{
    public static void Map(WebApplication app)
    {
        app.MapPost("/api/locker/v4/{deploymentId}/account/{accountId}/lock-in-immutable-item/{cosmeticItemId}",
            LockInImmutableItem);
        app.MapMethods("/api/locker/v4/{deploymentId}/account/{accountId}/companion-name",
            new[] { "PATCH" }, SetCompanionName);
        app.MapGet("/api/quest/v3/{deploymentId}/progress/account/{accountId}", GetQuestProgress);
        app.MapGet("/api/locker/v3/{deploymentId}/account/{accountId}/items", GetLockerV3Items);
        app.MapMethods("/api/locker/v3/{deploymentId}/loadout/{loadoutType}/account/{accountId}/active-loadout",
            new[] { "PUT" }, PutLockerV3ActiveLoadout);
        app.MapGet("/api/locker/v4/{deploymentId}/account/{accountId}/items", GetLockerV4Items);
        app.MapMethods("/api/locker/v4/{deploymentId}/account/{accountId}/active-loadout-group",
            new[] { "PUT" }, PutLockerV4ActiveLoadoutGroup);
    }

    private static async Task LockInImmutableItem(HttpContext ctx)
    {
        var profile = McpCommon.Profile("athena");
        var body = await RequestUtil.ReadObjectAsync(ctx);
        var changed = false;

        var cosmeticItemId = RequestUtil.Route(ctx, "cosmeticItemId") ?? "";
        var parts = cosmeticItemId.Split(':');
        var itemGuid = parts.Length > 2 ? parts[2] : "";

        if (!string.IsNullOrEmpty(itemGuid))
        {
            var item = McpCommon.Item(profile, itemGuid);
            if (item?["attributes"] is JsonObject attrs && body["variants"] is JsonObject variants)
            {
                if (attrs["variants"] is not JsonArray variantList)
                {
                    variantList = new JsonArray();
                    attrs["variants"] = variantList;
                }

                foreach (var pair in variants)
                {
                    variantList.Add(new JsonObject
                    {
                        ["channel"] = pair.Key,
                        ["active"] = (pair.Value as JsonObject)?.GetStr("variantTag"),
                        ["owned"] = new JsonArray()
                    });
                }

                attrs["locked_in"] = true;
                changed = true;
            }
        }

        if (changed)
        {
            profile["rvn"] = profile.GetLong("rvn") + 1;
            profile["commandRevision"] = profile.GetLong("commandRevision") + 1;
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Empty(ctx, 204);
    }

    private static async Task SetCompanionName(HttpContext ctx)
    {
        var profile = McpCommon.Profile("athena");
        var body = await RequestUtil.ReadObjectAsync(ctx);
        var changed = false;

        var cosmeticItemId = body.GetStr("cosmeticItemId");
        var companionName = body.GetStr("companionName");

        if (!string.IsNullOrEmpty(cosmeticItemId) && !string.IsNullOrEmpty(companionName))
        {
            var parts = cosmeticItemId.Split(':');
            var itemGuid = parts.Length > 2 ? parts[2] : "";
            var item = string.IsNullOrEmpty(itemGuid) ? null : McpCommon.Item(profile, itemGuid);

            if (item?["attributes"] is JsonObject attrs)
            {
                if (attrs["variants"] is not JsonArray variants)
                {
                    variants = new JsonArray();
                    attrs["variants"] = variants;
                }

                variants.Add(new JsonObject
                {
                    ["channel"] = "CustomName",
                    ["active"] = companionName,
                    ["owned"] = new JsonArray()
                });
                changed = true;
            }
        }

        if (changed)
        {
            profile["rvn"] = profile.GetLong("rvn") + 1;
            profile["commandRevision"] = profile.GetLong("commandRevision") + 1;
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Empty(ctx, 204);
    }

    private static async Task GetQuestProgress(HttpContext ctx)
    {
        var profile = McpCommon.Profile("athena");
        var memory = Functions.GetVersionInfo(ctx);

        var seasonPrefix = memory.Season < 10 ? $"0{memory.Season}" : memory.Season.ToString();
        var prefix = $"QS{seasonPrefix}-";

        var challengeBundleSchedules = new JsonArray();
        var challengeBundles = new JsonArray();
        var quests = new JsonArray();

        var items = McpCommon.Items(profile);
        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            if (!key.StartsWith(prefix, StringComparison.Ordinal)) continue;

            var item = items[key] as JsonObject;
            var templateId = item?.GetStr("templateId") ?? "";
            var attributes = item?["attributes"] as JsonObject ?? new JsonObject();
            var lower = templateId.ToLowerInvariant();

            if (lower.StartsWith("challengebundleschedule:"))
            {
                challengeBundleSchedules.Add(new JsonObject
                {
                    ["itemId"] = key,
                    ["templateId"] = templateId,
                    ["attributes"] = new JsonObject
                    {
                        ["unlock_epoch"] = attributes["unlock_epoch"]?.DeepClone(),
                        ["granted_bundles"] = attributes["granted_bundles"]?.DeepClone()
                    }
                });
            }
            else if (lower.StartsWith("challengebundle:"))
            {
                challengeBundles.Add(new JsonObject
                {
                    ["itemId"] = key,
                    ["templateId"] = templateId,
                    ["challengeScheduleId"] = attributes["challenge_bundle_schedule_id"]?.DeepClone(),
                    ["attributes"] = new JsonObject
                    {
                        ["num_granted_bundle_quests"] = attributes["num_granted_bundle_quests"]?.DeepClone(),
                        ["grantedquestinstanceids"] = attributes["grantedquestinstanceids"]?.DeepClone()
                    }
                });
            }
            else if (lower.StartsWith("quest:"))
            {
                var objectives = new JsonArray();
                foreach (var attr in attributes)
                {
                    if (!attr.Key.StartsWith("completion_", StringComparison.Ordinal)) continue;
                    objectives.Add(new JsonObject
                    {
                        ["statName"] = attr.Key.Substring("completion_".Length),
                        ["quantity"] = attr.Value?.DeepClone(),
                        ["stage"] = -1
                    });
                }

                quests.Add(new JsonObject
                {
                    ["templateId"] = templateId,
                    ["productTags"] = new JsonArray(),
                    ["state"] = attributes["quest_state"]?.DeepClone(),
                    ["objectives"] = objectives,
                    ["challengeBundleId"] = attributes["challenge_bundle_id"]?.DeepClone(),
                    ["itemId"] = key
                });
            }
        }

        var response = new JsonObject
        {
            ["questProgress"] = new JsonObject
            {
                ["challengeBundleSchedules"] = challengeBundleSchedules,
                ["challengeBundles"] = challengeBundles,
                ["quests"] = quests
            },
            ["tokens"] = new JsonArray(),
            ["accountXp"] = new JsonObject
            {
                ["dynamicXp"] = new JsonObject
                {
                    ["timespan"] = -1.0,
                    ["bucketXp"] = 0,
                    ["bankXp"] = 0,
                    ["bankXpMult"] = 1.0,
                    ["boosterBucketXp"] = 0,
                    ["boosterXpMult"] = 1.0,
                    ["weeklyExcessXpMult"] = 1.0,
                    ["currentWeekXp"] = 0,
                    ["currentWeek"] = 12
                },
                ["playtimeXp"] = new JsonObject
                {
                    ["currentWeek"] = 12,
                    ["currentWeekXp"] = 0
                },
                ["restedXp"] = 0,
                ["seasonXp"] = 100000,
                ["seasonLevel"] = 100,
                ["seasonNumber"] = memory.Season,
                ["seasonBegin"] = "2020-01-01T00:00:00Z",
                ["timeDilation"] = 0
            }
        };

        await ResponseUtil.Json(ctx, response);
    }

    private static async Task GetLockerV3Items(HttpContext ctx)
    {
        var profile = McpCommon.Profile("athena");
        var changed = false;
        var attrs = McpCommon.Attrs(profile);

        if (attrs["loadout_presets"] is not JsonObject presets)
        {
            presets = new JsonObject();
            attrs["loadout_presets"] = presets;
            changed = true;
        }

        var deploymentId = RequestUtil.Route(ctx, "deploymentId");
        var accountId = RequestUtil.Route(ctx, "accountId");
        var activeLoadouts = new JsonArray();

        foreach (var cosmeticPair in presets.ToList())
        {
            if (cosmeticPair.Value is not JsonObject loadoutPresets) continue;

            foreach (var loadoutPair in loadoutPresets.ToList())
            {
                var loadoutId = J.AsString(loadoutPair.Value);
                if (loadoutId == null) continue;

                var loadoutItem = McpCommon.Item(profile, loadoutId);
                if (loadoutItem == null) continue;

                var date = Functions.IsoNow();
                var loadoutToAdd = new JsonObject
                {
                    ["deploymentId"] = deploymentId,
                    ["accountId"] = accountId,
                    ["loadoutType"] = loadoutItem.GetStr("templateId"),
                    ["loadoutShuffleType"] = "DISABLED",
                    ["athenaItemId"] = loadoutId,
                    ["creationTime"] = date,
                    ["updatedTime"] = date,
                    ["loadoutSlots"] = new JsonArray()
                };

                var slots = (loadoutItem["attributes"] as JsonObject)?["slots"] as JsonArray;
                if (slots != null)
                {
                    foreach (var slotNode in slots)
                    {
                        if (slotNode is not JsonObject slot) continue;

                        var slotToAdd = new JsonObject
                        {
                            ["slotTemplate"] = slot["slot_template"]?.DeepClone(),
                            ["equippedItemId"] = slot["equipped_item"]?.DeepClone(),
                            ["itemCustomizations"] = new JsonArray()
                        };

                        var customizations = (JsonArray)slotToAdd["itemCustomizations"]!;
                        if (slot["customization_info"] is JsonArray customizationInfo)
                        {
                            foreach (var customizationNode in customizationInfo)
                            {
                                if (customizationNode is not JsonObject customization) continue;

                                customizations.Add(new JsonObject
                                {
                                    ["channelTag"] = customization["channel_tag"]?.DeepClone(),
                                    ["variantTag"] = customization["variant_tag"]?.DeepClone(),
                                    ["additionalData"] = customization["additional_data"]?.DeepClone()
                                });
                            }
                        }

                        ((JsonArray)loadoutToAdd["loadoutSlots"]!).Add(slotToAdd);
                    }
                }

                activeLoadouts.Add(loadoutToAdd);
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Json(ctx, new JsonObject
        {
            ["activeLoadouts"] = activeLoadouts,
            ["loadoutPresets"] = new JsonArray()
        });
    }

    private static async Task PutLockerV3ActiveLoadout(HttpContext ctx)
    {
        var body = await RequestUtil.ReadObjectAsync(ctx);
        var profile = McpCommon.Profile("athena");
        var changed = false;
        var attrs = McpCommon.Attrs(profile);
        var items = McpCommon.Items(profile);

        var deploymentId = RequestUtil.Route(ctx, "deploymentId");
        var accountId = RequestUtil.Route(ctx, "accountId");
        var date = Functions.IsoNow();

        var response = new JsonObject
        {
            ["deploymentId"] = deploymentId,
            ["accountId"] = accountId,
            ["loadoutType"] = body["loadoutType"]?.DeepClone(),
            ["loadoutShuffleType"] = "DISABLED",
            ["athenaItemId"] = body["athenaItemId"]?.DeepClone(),
            ["creationTime"] = date,
            ["updatedTime"] = date,
            ["loadoutSlots"] = body["loadoutSlots"]?.DeepClone()
        };

        var loadoutData = new JsonObject { ["slots"] = new JsonArray() };
        var slots = (JsonArray)loadoutData["slots"]!;

        if (body["loadoutSlots"] is JsonArray bodySlots)
        {
            foreach (var slotNode in bodySlots)
            {
                if (slotNode is not JsonObject slot) continue;

                var slotToAdd = new JsonObject();
                if (!string.IsNullOrEmpty(slot.GetStr("slotTemplate")))
                    slotToAdd["slot_template"] = slot["slotTemplate"]?.DeepClone();
                if (!string.IsNullOrEmpty(slot.GetStr("equippedItemId")))
                    slotToAdd["equipped_item"] = slot["equippedItemId"]?.DeepClone();

                if (slot["itemCustomizations"] is JsonArray itemCustomizations)
                {
                    foreach (var customizationNode in itemCustomizations)
                    {
                        if (customizationNode is not JsonObject customization) continue;

                        var customizationToAdd = new JsonObject();
                        if (!string.IsNullOrEmpty(customization.GetStr("channelTag")))
                            customizationToAdd["channel_tag"] = customization["channelTag"]?.DeepClone();
                        if (!string.IsNullOrEmpty(customization.GetStr("variantTag")))
                            customizationToAdd["variant_tag"] = customization["variantTag"]?.DeepClone();
                        if (!string.IsNullOrEmpty(customization.GetStr("additionalData")))
                            customizationToAdd["additional_data"] = customization["additionalData"]?.DeepClone();

                        if (slotToAdd["customization_info"] is not JsonArray list)
                        {
                            list = new JsonArray();
                            slotToAdd["customization_info"] = list;
                        }
                        list.Add(customizationToAdd);
                    }
                }

                slots.Add(slotToAdd);
            }
        }

        if (attrs["loadout_presets"] is not JsonObject presets)
        {
            presets = new JsonObject();
            attrs["loadout_presets"] = presets;
            changed = true;
        }

        var loadoutType = body.GetStr("loadoutType");
        var presetIndex = body.GetStr("presetIndex");

        if (loadoutType != null && !presets.ContainsKey(loadoutType))
        {
            var newLoadoutId = Functions.MakeId();
            items[newLoadoutId] = McpCommon.NewItem(loadoutType, new JsonObject());
            presets[loadoutType] = new JsonObject { [presetIndex ?? ""] = newLoadoutId };
            changed = true;
        }

        try
        {
            var loadoutGuid = (presets[loadoutType ?? ""] as JsonObject)?[presetIndex ?? ""] is JsonValue guid
                ? J.AsString(guid)
                : null;

            if (loadoutGuid != null && items[loadoutGuid] is JsonObject loadoutItem)
            {
                loadoutItem["attributes"] = loadoutData.DeepClone();
                changed = true;
            }
        }
        catch
        {
            
        }

        ApplyLoadoutCustomizations(items, loadoutData, ref changed);

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Json(ctx, response);
    }

    private static async Task GetLockerV4Items(HttpContext ctx)
    {
        var profile = McpCommon.Profile("athena");
        var changed = false;
        var attrs = McpCommon.Attrs(profile);

        if (attrs["loadout_presets"] is not JsonObject presets)
        {
            presets = new JsonObject();
            attrs["loadout_presets"] = presets;
            changed = true;
        }

        var deploymentId = RequestUtil.Route(ctx, "deploymentId");
        var accountId = RequestUtil.Route(ctx, "accountId");

        var activeLoadoutGroup = new JsonObject
        {
            ["accountId"] = accountId,
            ["athenaItemId"] = "lawinsathenaitemidlol",
            ["creationTime"] = Functions.IsoNow(),
            ["deploymentId"] = deploymentId,
            ["loadouts"] = new JsonObject()
        };
        var loadoutPresetsResponse = new JsonArray();

        foreach (var cosmeticPair in presets.ToList())
        {
            if (cosmeticPair.Value is not JsonObject loadoutPresets) continue;

            foreach (var loadoutPair in loadoutPresets.ToList())
            {
                var loadoutId = J.AsString(loadoutPair.Value);
                if (loadoutId == null) continue;

                var loadoutItem = McpCommon.Item(profile, loadoutId);
                if (loadoutItem == null) continue;

                var date = Functions.IsoNow();
                var activeCosmeticLoadout = new JsonObject
                {
                    ["loadoutSlots"] = new JsonArray(),
                    ["shuffleType"] = "DISABLED"
                };

                var loadoutToAdd = new JsonObject
                {
                    ["deploymentId"] = deploymentId,
                    ["accountId"] = accountId,
                    ["loadoutType"] = loadoutItem.GetStr("templateId"),
                    ["loadoutShuffleType"] = "DISABLED",
                    ["athenaItemId"] = loadoutId,
                    ["creationTime"] = date,
                    ["updatedTime"] = date,
                    ["loadoutSlots"] = new JsonArray()
                };

                var slots = (loadoutItem["attributes"] as JsonObject)?["slots"] as JsonArray;
                if (slots != null)
                {
                    foreach (var slotNode in slots)
                    {
                        if (slotNode is not JsonObject slot) continue;

                        var slotToAdd = new JsonObject
                        {
                            ["slotTemplate"] = slot["slot_template"]?.DeepClone(),
                            ["equippedItemId"] = slot["equipped_item"]?.DeepClone(),
                            ["itemCustomizations"] = new JsonArray()
                        };

                        var customizations = (JsonArray)slotToAdd["itemCustomizations"]!;
                        if (slot["customization_info"] is JsonArray customizationInfo)
                        {
                            foreach (var customizationNode in customizationInfo)
                            {
                                if (customizationNode is not JsonObject customization) continue;

                                customizations.Add(new JsonObject
                                {
                                    ["channelTag"] = customization["channel_tag"]?.DeepClone(),
                                    ["variantTag"] = customization["variant_tag"]?.DeepClone(),
                                    ["additionalData"] = customization["additional_data"]?.DeepClone()
                                });
                            }
                        }

                        ((JsonArray)loadoutToAdd["loadoutSlots"]!).Add(slotToAdd);
                        ((JsonArray)activeCosmeticLoadout["loadoutSlots"]!).Add(slotToAdd?.DeepClone());
                    }
                }

                loadoutPresetsResponse.Add(loadoutToAdd);
                ((JsonObject)activeLoadoutGroup["loadouts"]!)[loadoutItem.GetStr("templateId") ?? ""] =
                    activeCosmeticLoadout;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Json(ctx, new JsonObject
        {
            ["activeLoadoutGroup"] = activeLoadoutGroup,
            ["loadoutGroupPresets"] = new JsonArray(),
            ["loadoutPresets"] = loadoutPresetsResponse
        });
    }

    private static async Task PutLockerV4ActiveLoadoutGroup(HttpContext ctx)
    {
        var body = await RequestUtil.ReadObjectAsync(ctx);
        var profile = McpCommon.Profile("athena");
        var changed = false;
        var attrs = McpCommon.Attrs(profile);
        var items = McpCommon.Items(profile);

        var deploymentId = RequestUtil.Route(ctx, "deploymentId");
        var accountId = RequestUtil.Route(ctx, "accountId");
        var date = Functions.IsoNow();

        if (attrs["loadout_presets"] is not JsonObject presets)
        {
            presets = new JsonObject();
            attrs["loadout_presets"] = presets;
            changed = true;
        }

        if (body["loadouts"] is JsonObject loadouts)
        {
            foreach (var loadoutPair in loadouts.ToList())
            {
                var loadoutType = loadoutPair.Key;
                if (loadoutPair.Value is not JsonObject loadout) continue;

                var loadoutData = new JsonObject { ["slots"] = new JsonArray() };
                var slots = (JsonArray)loadoutData["slots"]!;

                if (loadout["loadoutSlots"] is JsonArray loadoutSlots)
                {
                    foreach (var slotNode in loadoutSlots)
                    {
                        if (slotNode is not JsonObject slot) continue;

                        var slotToAdd = new JsonObject();
                        if (!string.IsNullOrEmpty(slot.GetStr("slotTemplate")))
                            slotToAdd["slot_template"] = slot["slotTemplate"]?.DeepClone();

                        var equipped = slot.GetStr("equippedItemId");
                        if (equipped != null)
                        {
                            equipped = string.Join(":", equipped.Split(':').Take(2));
                            slot["equippedItemId"] = equipped;
                            slotToAdd["equipped_item"] = equipped;
                        }

                        if (slot["itemCustomizations"] is JsonArray itemCustomizations)
                        {
                            foreach (var customizationNode in itemCustomizations)
                            {
                                if (customizationNode is not JsonObject customization) continue;

                                var customizationToAdd = new JsonObject();
                                if (!string.IsNullOrEmpty(customization.GetStr("channelTag")))
                                    customizationToAdd["channel_tag"] = customization["channelTag"]?.DeepClone();
                                if (!string.IsNullOrEmpty(customization.GetStr("variantTag")))
                                    customizationToAdd["variant_tag"] = customization["variantTag"]?.DeepClone();
                                if (!string.IsNullOrEmpty(customization.GetStr("additionalData")))
                                    customizationToAdd["additional_data"] = customization["additionalData"]?.DeepClone();

                                if (slotToAdd["customization_info"] is not JsonArray list)
                                {
                                    list = new JsonArray();
                                    slotToAdd["customization_info"] = list;
                                }
                                list.Add(customizationToAdd);
                            }
                        }

                        slots.Add(slotToAdd);
                    }
                }

                if (!presets.ContainsKey(loadoutType))
                {
                    var newLoadoutId = Functions.MakeId();
                    items[newLoadoutId] = McpCommon.NewItem(loadoutType, new JsonObject());
                    presets[loadoutType] = new JsonObject { ["0"] = newLoadoutId };
                    changed = true;
                }

                try
                {
                    var loadoutGuid = (presets[loadoutType] as JsonObject)?["0"] is JsonValue guid
                        ? J.AsString(guid)
                        : null;

                    if (loadoutGuid != null && items[loadoutGuid] is JsonObject loadoutItem)
                    {
                        loadoutItem["attributes"] = loadoutData.DeepClone();
                        changed = true;
                    }
                }
                catch
                {
                    
                }

                ApplyLoadoutCustomizations(items, loadoutData, ref changed);
            }
        }

        var response = new JsonObject
        {
            ["deploymentId"] = deploymentId,
            ["accountId"] = accountId,
            ["athenaItemId"] = "lawinsathenaitemidlol",
            ["creationTime"] = date,
            ["updatedTime"] = date,
            ["loadouts"] = body["loadouts"]?.DeepClone(),
            ["shuffleType"] = "DISABLED"
        };

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save("athena", profile);
        }

        await ResponseUtil.Json(ctx, response);
    }

    private static void ApplyLoadoutCustomizations(JsonObject items, JsonObject loadoutData, ref bool changed)
    {
        if (loadoutData["slots"] is not JsonArray slots) return;

        foreach (var slotNode in slots)
        {
            if (slotNode is not JsonObject slot) continue;
            if (!slot.ContainsKey("customization_info")) continue;

            var equipped = slot.GetStr("equipped_item");
            if (equipped == null) continue;

            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var item = items[key] as JsonObject;
                if ((item?.GetStr("templateId") ?? "").ToLowerInvariant() != equipped.ToLowerInvariant()) continue;

                if (item!["attributes"] is not JsonObject itemAttrs) continue;
                if (itemAttrs["variants"] is not JsonArray variants)
                {
                    variants = new JsonArray();
                    itemAttrs["variants"] = variants;
                }

                if (slot["customization_info"] is JsonArray customizations)
                {
                    foreach (var customizationNode in customizations)
                    {
                        if (customizationNode is not JsonObject customization) continue;

                        var channelTag = customization.GetStr("channel_tag");
                        var active = $"{customization.GetStr("variant_tag")}.{customization.GetStr("additional_data")}";

                        var found = false;
                        foreach (var variantNode in variants)
                        {
                            if (variantNode is not JsonObject variant) continue;
                            if (variant.GetStr("channel") != channelTag) continue;

                            variant["active"] = active;
                            found = true;
                            break;
                        }

                        if (!found)
                        {
                            variants.Add(new JsonObject
                            {
                                ["channel"] = channelTag,
                                ["active"] = active,
                                ["owned"] = new JsonArray()
                            });
                        }
                    }
                }

                changed = true;
            }
        }
    }
}
