using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpItemUpgradeHandlers
{
    public static async Task UpgradeItem(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs != null)
            {
                attrs["level"] = attrs.GetLong("level") + 1;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "level", ItemAttrs(profile, targetItemId!)?["level"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task UpgradeSlottedItem(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "collection_book_people0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs != null)
            {
                var desiredLevel = body.GetLong("desiredLevel");
                attrs["level"] = desiredLevel != 0 ? desiredLevel : attrs.GetLong("level") + 1;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "level", ItemAttrs(profile, targetItemId!)?["level"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task UpgradeItemBulk(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs != null)
            {
                attrs["level"] = body.GetLong("desiredLevel");
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "level", ItemAttrs(profile, targetItemId!)?["level"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task ConvertItem(HttpContext ctx)
    {
        await Convert(ctx, "campaign");
    }

    public static async Task ConvertSlottedItem(HttpContext ctx)
    {
        await Convert(ctx, "collection_book_people0");
    }

    private static async Task Convert(HttpContext ctx, string defaultProfile)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, defaultProfile);
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        var item = string.IsNullOrEmpty(targetItemId) ? null : McpCommon.Item(profile, targetItemId);
        if (item != null)
        {
            var templateId = item.GetStr("templateId") ?? "";
            var lower = templateId.ToLowerInvariant();

            if (lower.Contains("t04")) templateId = Regex.Replace(templateId, "t04", "T05", RegexOptions.IgnoreCase);
            if (lower.Contains("t03")) templateId = Regex.Replace(templateId, "t03", "T04", RegexOptions.IgnoreCase);
            if (lower.Contains("t02")) templateId = Regex.Replace(templateId, "t02", "T03", RegexOptions.IgnoreCase);
            if (lower.Contains("t01")) templateId = Regex.Replace(templateId, "t01", "T02", RegexOptions.IgnoreCase);

            if (body.GetLong("conversionIndex") == 1)
                templateId = Regex.Replace(templateId, "ore", "Crystal", RegexOptions.IgnoreCase);

            item["templateId"] = templateId;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            var id = Functions.MakeId();
            var items = McpCommon.Items(profile);
            items[id] = items[targetItemId!]?.DeepClone();
            changes.Add(McpCommon.ItemAdded(id, items[id]));

            items.Remove(targetItemId!);
            changes.Add(McpCommon.ItemRemoved(targetItemId!));

            var newItem = McpCommon.Item(profile, id);
            notifications.Add(new JsonObject
            {
                ["type"] = "conversionResult",
                ["primary"] = true,
                ["itemsGranted"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["itemType"] = newItem?.GetStr("templateId"),
                        ["itemGuid"] = id,
                        ["itemProfile"] = profileId,
                        ["attributes"] = new JsonObject
                        {
                            ["level"] = newItem?["attributes"]?["level"]?.DeepClone(),
                            ["alterations"] = newItem?["attributes"]?["alterations"]?.DeepClone() ?? new JsonArray()
                        },
                        ["quantity"] = 1
                    }
                }
            });

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task UpgradeItemRarity(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        var item = string.IsNullOrEmpty(targetItemId) ? null : McpCommon.Item(profile, targetItemId);
        if (item != null)
        {
            var templateId = item.GetStr("templateId") ?? "";
            var lower = templateId.ToLowerInvariant();

            if (lower.Contains("_vr_")) templateId = Regex.Replace(templateId, "_vr_", "_SR_", RegexOptions.IgnoreCase);
            if (lower.Contains("_r_")) templateId = Regex.Replace(templateId, "_r_", "_VR_", RegexOptions.IgnoreCase);
            if (lower.Contains("_uc_")) templateId = Regex.Replace(templateId, "_uc_", "_R_", RegexOptions.IgnoreCase);
            if (lower.Contains("_c_")) templateId = Regex.Replace(templateId, "_c_", "_UC_", RegexOptions.IgnoreCase);

            item["templateId"] = templateId;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            var id = Functions.MakeId();
            var items = McpCommon.Items(profile);
            items[id] = items[targetItemId!]?.DeepClone();
            changes.Add(McpCommon.ItemAdded(id, items[id]));

            items.Remove(targetItemId!);
            changes.Add(McpCommon.ItemRemoved(targetItemId!));

            var newItem = McpCommon.Item(profile, id);
            notifications.Add(new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "upgradeItemRarityNotification",
                    ["primary"] = true,
                    ["itemsGranted"] = new JsonArray
                    {
                        new JsonObject
                        {
                            ["itemType"] = newItem?.GetStr("templateId"),
                            ["itemGuid"] = id,
                            ["itemProfile"] = profileId,
                            ["attributes"] = new JsonObject
                            {
                                ["level"] = newItem?["attributes"]?["level"]?.DeepClone(),
                                ["alterations"] = newItem?["attributes"]?["alterations"]?.DeepClone() ?? new JsonArray()
                            },
                            ["quantity"] = 1
                        }
                    }
                }
            });

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task PromoteItem(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs != null)
            {
                attrs["level"] = attrs.GetLong("level") + 2;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "level", ItemAttrs(profile, targetItemId!)?["level"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task TransmogItem(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        JsonArray transformIds = null;
        if (body["sacrificeItemIds"] is JsonArray sacrifice && !string.IsNullOrEmpty(body.GetStr("transmogKeyTemplateId")))
        {
            var items = McpCommon.Items(profile);
            foreach (var idNode in sacrifice)
            {
                var id = J.AsString(idNode);
                if (id == null) continue;
                items.Remove(id);
                changes.Add(McpCommon.ItemRemoved(id));
            }

            var transformData = DataStore.Response("Campaign/transformItemIDS.json") as JsonObject;
            var key = body.GetStr("transmogKeyTemplateId")!;
            if (transformData != null && transformData.ContainsKey(key))
                transformIds = transformData[key] as JsonArray;
            else
                transformIds = (DataStore.Response("Campaign/cardPackData.json") as JsonObject)?["default"] as JsonArray;

            changed = true;
        }

        if (changed && transformIds is { Count: > 0 })
        {
            McpCommon.Increment(profile);

            var random = Random.Shared.Next(transformIds.Count);
            var templateId = J.AsString(transformIds[random]) ?? "";
            var id = Functions.MakeId();

            JsonObject attributes;
            if (templateId.ToLowerInvariant().StartsWith("worker:"))
            {
                attributes = Functions.MakeSurvivorAttributes(templateId);
            }
            else
            {
                attributes = new JsonObject
                {
                    ["legacy_alterations"] = new JsonArray(),
                    ["max_level_bonus"] = 0,
                    ["level"] = 1,
                    ["refund_legacy_item"] = false,
                    ["item_seen"] = false,
                    ["alterations"] = new JsonArray("", "", "", "", "", ""),
                    ["xp"] = 0,
                    ["refundable"] = false,
                    ["alteration_base_rarities"] = new JsonArray(),
                    ["favorite"] = false
                };
            }

            var item = McpCommon.NewItem(templateId, attributes);
            McpCommon.Items(profile)[id] = item;

            notifications.Add(new JsonObject
            {
                ["type"] = "transmogResult",
                ["primary"] = true,
                ["transmoggedItems"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["itemType"] = item.GetStr("templateId"),
                        ["itemGuid"] = id,
                        ["itemProfile"] = profileId,
                        ["attributes"] = item["attributes"]?.DeepClone(),
                        ["quantity"] = 1
                    }
                }
            });

            changes.Add(McpCommon.ItemAdded(id, item));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task RedeemResearchToken(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body.ContainsKey("index"))
        {
            var itemId = Functions.MakeId();
            var items = McpCommon.Items(profile);
            string researchTokenId = null;

            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                if ((items[key] as JsonObject)?.GetStr("templateId")?.ToLowerInvariant() == "token:campaignresearchtoken")
                {
                    researchTokenId = key;
                    break;
                }
            }

            if (researchTokenId != null &&
                (items[researchTokenId] as JsonObject)?["attributes"] is JsonObject tokenAttrs &&
                tokenAttrs["items_for_schematic_creation_data"] is JsonArray tokenItems)
            {
                var index = (int)body.GetLong("index");
                if (index >= 0 && index < tokenItems.Count && tokenItems[index] is JsonObject selectedItem)
                {
                    var selectedId = selectedItem.GetStr("itemId") ?? "";
                    selectedId = Regex.Replace(selectedId, "Weapon:wid_", "Schematic:sid_", RegexOptions.IgnoreCase);
                    selectedId = Regex.Replace(selectedId, "Trap:tid_", "Schematic:sid_", RegexOptions.IgnoreCase);
                    selectedItem["itemId"] = selectedId;

                    var split = selectedId.ToLowerInvariant().Split('_');
                    var tierMap = new Dictionary<string, string>
                    {
                        ["t01"] = "i", ["t02"] = "ii", ["t03"] = "iii",
                        ["t04"] = "iv", ["t05"] = "v", ["t06"] = "vi"
                    };
                    var rarityMap = new Dictionary<string, string>
                    {
                        ["c"] = "Common", ["uc"] = "Uncommon", ["r"] = "Rare",
                        ["vr"] = "Epic", ["sr"] = "Legendary", ["ur"] = "Mythic"
                    };

                    var lastPart = split.Length > 0 ? split[^1] : "";
                    var rarityPart = split.FirstOrDefault(rarityMap.ContainsKey) ?? "";

                    var newItem = new JsonObject
                    {
                        ["templateId"] = selectedId,
                        ["attributes"] = new JsonObject
                        {
                            ["starting_level"] = selectedItem["itemLevel"]?.DeepClone(),
                            ["level"] = selectedItem["itemLevel"]?.DeepClone(),
                            ["item_seen"] = false,
                            ["starting_tier"] = tierMap.TryGetValue(lastPart, out var tier) ? tier : null,
                            ["alterations"] = selectedItem["itemPerks"]?.DeepClone(),
                            ["starting_rarity"] = rarityMap.TryGetValue(rarityPart, out var rarity) ? rarity : null
                        },
                        ["quantity"] = 1
                    };

                    var templateId = newItem.GetStr("templateId") ?? "";
                    if (templateId.ToLowerInvariant().StartsWith("schematic:sid_pistol_auto_vacuumtube_"))
                    {
                        templateId = $"Schematic:SID_Pistol_VacuumTube_Auto_{templateId.Substring(37)}";
                        newItem["templateId"] = templateId;
                    }
                    if (templateId.ToLowerInvariant().StartsWith("schematic:sid_launcher_wintergrenade_"))
                    {
                        templateId = $"Schematic:SID_Launcher_Grenade_Winter_{templateId.Substring(37)}";
                        newItem["templateId"] = templateId;
                    }

                    items[itemId] = newItem;
                    changes.Add(McpCommon.ItemAdded(itemId, newItem));

                    items.Remove(researchTokenId);
                    changes.Add(McpCommon.ItemRemoved(researchTokenId));

                    changed = true;
                }
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task RespecAlteration(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        var alterationId = body.GetStr("alterationId");
        if (!string.IsNullOrEmpty(targetItemId) && !string.IsNullOrEmpty(alterationId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs != null)
            {
                if (attrs["alterations"] is not JsonArray alterations)
                {
                    alterations = new JsonArray("", "", "", "", "", "");
                    attrs["alterations"] = alterations;
                }

                var slot = (int)body.GetLong("alterationSlot");
                while (alterations.Count <= slot) alterations.Add("");
                alterations[slot] = alterationId;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "alterations", ItemAttrs(profile, targetItemId!)?["alterations"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task UpgradeAlteration(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var attrs = ItemAttrs(profile, targetItemId);
            if (attrs?["alterations"] is JsonArray alterations)
            {
                var slot = (int)body.GetLong("alterationSlot");
                if (slot >= 0 && slot < alterations.Count)
                {
                    var value = J.AsString(alterations[slot]) ?? "";
                    var lower = value.ToLowerInvariant();

                    if (lower.Contains("t04")) value = Regex.Replace(value, "t04", "T05", RegexOptions.IgnoreCase);
                    if (lower.Contains("t03")) value = Regex.Replace(value, "t03", "T04", RegexOptions.IgnoreCase);
                    if (lower.Contains("t02")) value = Regex.Replace(value, "t02", "T03", RegexOptions.IgnoreCase);
                    if (lower.Contains("t01")) value = Regex.Replace(value, "t01", "T02", RegexOptions.IgnoreCase);

                    alterations[slot] = value;
                }

                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(targetItemId!, "alterations", ItemAttrs(profile, targetItemId!)?["alterations"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task RespecResearch(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var attrs = McpCommon.Attrs(profile);
        if (attrs["research_levels"] is JsonObject researchLevels)
        {
            researchLevels["technology"] = 0;
            researchLevels["fortitude"] = 0;
            researchLevels["offense"] = 0;
            researchLevels["resistance"] = 0;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("research_levels", McpCommon.Attrs(profile)["research_levels"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task RespecUpgrades(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");

        var items = McpCommon.Items(profile);
        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            var item = items[key] as JsonObject;
            if (!(item?.GetStr("templateId") ?? "").ToLowerInvariant().StartsWith("homebasenode:skilltree_")) continue;

            item!["quantity"] = 0;
            changes.Add(McpCommon.ItemQuantityChanged(key, 0));
        }

        McpCommon.Increment(profile);
        McpCommon.Save(profileId, profile);

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task PurchaseResearchStatUpgrade(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var statId = body.GetStr("statId");
        var attrs = McpCommon.Attrs(profile);
        if (!string.IsNullOrEmpty(statId) && attrs["research_levels"] is JsonObject researchLevels)
        {
            researchLevels[statId] = researchLevels.GetLong(statId) + 1;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("research_levels", McpCommon.Attrs(profile)["research_levels"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task PurchaseOrUpgradeHomebaseNode(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var createNode = true;

        var nodeId = body.GetStr("nodeId");
        if (!string.IsNullOrEmpty(nodeId))
        {
            var items = McpCommon.Items(profile);
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var item = items[key] as JsonObject;
                if ((item?.GetStr("templateId") ?? "").ToLowerInvariant() != nodeId.ToLowerInvariant()) continue;

                item!["quantity"] = item.GetLong("quantity") + 1;
                changes.Add(McpCommon.ItemQuantityChanged(key, item.GetLong("quantity")));
                createNode = false;
            }

            if (createNode)
            {
                var id = Functions.MakeId();
                items[id] = McpCommon.NewItem(nodeId, new JsonObject { ["item_seen"] = false });
                changes.Add(McpCommon.ItemAdded(id, items[id]));
            }
        }

        McpCommon.Increment(profile);
        McpCommon.Save(profileId, profile);

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    private static JsonObject ItemAttrs(JsonObject profile, string itemId)
    {
        var item = McpCommon.Item(profile, itemId);
        if (item == null) return null;
        if (item["attributes"] is not JsonObject attrs)
        {
            attrs = new JsonObject();
            item["attributes"] = attrs;
        }
        return attrs;
    }
}
