using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpInventoryHandlers
{
    public static async Task CraftWorldItem(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var memory = Functions.GetVersionInfo(ctx);
        var profileId = McpCommon.ProfileId(ctx, "theater0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var chosenProfile = false;
        JsonObject schematicProfile = null;

        if (4 <= memory.Season || memory.Build == 3.5 || (memory.Build == 3.6 && !chosenProfile))
        {
            schematicProfile = McpCommon.Profile("campaign");
            chosenProfile = true;
        }

        if (3 >= memory.Season && !chosenProfile)
        {
            schematicProfile = McpCommon.Profile("profile0");
            chosenProfile = true;
        }

        var id = Functions.MakeId();
        var targetSchematicId = body.GetStr("targetSchematicItemId");

        if (!string.IsNullOrEmpty(targetSchematicId) && schematicProfile != null)
        {
            var source = McpCommon.Item(schematicProfile, targetSchematicId);
            if (source != null)
            {
                var item = (JsonObject)source.DeepClone();
                var templateId = item.GetStr("templateId") ?? "";
                var oldAttrs = item["attributes"] as JsonObject ?? new JsonObject();

                var itemType = "Weapon:";
                var itemIdType = "WID_";
                var parts = templateId.Split('_');
                var second = parts.Length > 1 ? parts[1].Split('_')[0].ToLowerInvariant() : "";
                if (second == "wall" || second == "floor" || second == "ceiling")
                {
                    itemType = "Trap:";
                    itemIdType = "TID_";
                }

                if (templateId.ToLowerInvariant().StartsWith("schematic:sid_pistol_vacuumtube_auto_"))
                    templateId = $"Schematic:SID_Pistol_Auto_VacuumTube_{templateId.Substring(37)}";
                if (templateId.ToLowerInvariant().StartsWith("schematic:sid_launcher_grenade_winter_"))
                    templateId = $"Schematic:SID_Launcher_WinterGrenade_{templateId.Substring(38)}";

                templateId = Regex.Replace(templateId, "schematic:", itemType, RegexOptions.IgnoreCase);
                templateId = Regex.Replace(templateId, "sid_", itemIdType, RegexOptions.IgnoreCase);

                item["templateId"] = templateId;
                var numTimes = body.GetLong("numTimesToCraft");
                item["quantity"] = numTimes != 0 ? numTimes : 1;

                var tier = body.GetStr("targetSchematicTier")?.ToLowerInvariant();
                if (!string.IsNullOrEmpty(tier))
                {
                    switch (tier)
                    {
                        case "i":
                            if (!templateId.ToLowerInvariant().Contains("t01")) oldAttrs["level"] = 10;
                            templateId = templateId.Substring(0, Math.Max(0, templateId.Length - 3)) + "T01";
                            templateId = Regex.Replace(templateId, "_crystal_", "_Ore_", RegexOptions.IgnoreCase);
                            break;
                        case "ii":
                            if (!templateId.ToLowerInvariant().Contains("t02")) oldAttrs["level"] = 20;
                            templateId = templateId.Substring(0, Math.Max(0, templateId.Length - 3)) + "T02";
                            templateId = Regex.Replace(templateId, "_crystal_", "_Ore_", RegexOptions.IgnoreCase);
                            break;
                        case "iii":
                            if (!templateId.ToLowerInvariant().Contains("t03")) oldAttrs["level"] = 30;
                            templateId = templateId.Substring(0, Math.Max(0, templateId.Length - 3)) + "T03";
                            templateId = Regex.Replace(templateId, "_crystal_", "_Ore_", RegexOptions.IgnoreCase);
                            break;
                        case "iv":
                            if (!templateId.ToLowerInvariant().Contains("t04")) oldAttrs["level"] = 40;
                            templateId = templateId.Substring(0, Math.Max(0, templateId.Length - 3)) + "T04";
                            break;
                        case "v":
                            templateId = templateId.Substring(0, Math.Max(0, templateId.Length - 3)) + "T05";
                            break;
                    }
                }

                item["templateId"] = templateId;
                item["attributes"] = new JsonObject
                {
                    ["clipSizeScale"] = 0,
                    ["loadedAmmo"] = 999,
                    ["level"] = oldAttrs["level"]?.DeepClone() ?? 1,
                    ["alterationDefinitions"] = oldAttrs["alterations"]?.DeepClone() ?? new JsonArray(),
                    ["baseClipSize"] = 999,
                    ["durability"] = 375,
                    ["itemSource"] = ""
                };

                McpCommon.Items(profile)[id] = item;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            var item = McpCommon.Item(profile, id);
            changes.Add(McpCommon.ItemAdded(id, item));

            notifications.Add(new JsonObject
            {
                ["type"] = "craftingResult",
                ["primary"] = true,
                ["itemsCrafted"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["itemType"] = item?.GetStr("templateId"),
                        ["itemGuid"] = id,
                        ["itemProfile"] = profileId,
                        ["attributes"] = new JsonObject
                        {
                            ["loadedAmmo"] = item?["attributes"]?["loadedAmmo"]?.DeepClone(),
                            ["level"] = item?["attributes"]?["level"]?.DeepClone(),
                            ["alterationDefinitions"] = item?["attributes"]?["alterationDefinitions"]?.DeepClone(),
                            ["durability"] = item?["attributes"]?["durability"]?.DeepClone()
                        },
                        ["quantity"] = item?.GetLong("quantity") ?? 1
                    }
                }
            });

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task DestroyWorldItems(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "theater0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["itemIds"] is JsonArray ids)
        {
            var items = McpCommon.Items(profile);
            foreach (var idNode in ids)
            {
                var id = J.AsString(idNode);
                if (id == null) continue;
                items.Remove(id);
                changes.Add(McpCommon.ItemRemoved(id));
            }
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task DisassembleWorldItems(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "theater0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["targetItemIdAndQuantityPairs"] is JsonArray pairs)
        {
            var items = McpCommon.Items(profile);
            foreach (var pairNode in pairs)
            {
                if (pairNode is not JsonObject pair) continue;
                var id = pair.GetStr("itemId");
                if (string.IsNullOrEmpty(id) || items[id] is not JsonObject item) continue;

                var quantity = pair.GetLong("quantity");
                var original = item.GetLong("quantity");

                if (quantity >= original)
                {
                    items.Remove(id);
                    changes.Add(McpCommon.ItemRemoved(id));
                }
                else
                {
                    item["quantity"] = original - quantity;
                    changes.Add(McpCommon.ItemQuantityChanged(id, item.GetLong("quantity")));
                }
            }
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task StorageTransfer(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var theater = McpCommon.Profile("theater0");
        var outpost = McpCommon.Profile("outpost0");
        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var baseRevision = theater.GetLong("rvn");
        var changed = false;

        JsonObject multi = null;

        if (body["transferOperations"] is JsonArray operations)
        {
            multi = new JsonObject
            {
                ["profileRevision"] = outpost.GetLong("rvn"),
                ["profileId"] = "outpost0",
                ["profileChangesBaseRevision"] = outpost.GetLong("rvn"),
                ["profileChanges"] = new JsonArray(),
                ["profileCommandRevision"] = outpost.GetLong("commandRevision")
            };
            multiUpdate.Add(multi);
            var outpostChanges = (JsonArray)multi["profileChanges"]!;

            foreach (var operationNode in operations)
            {
                if (operationNode is not JsonObject operation) continue;
                var id = operation.GetStr("itemId");
                if (string.IsNullOrEmpty(id)) continue;
                var quantity = operation.GetLong("quantity");

                if (!operation.GetBool("toStorage"))
                    Transfer(outpost, theater, outpostChanges, changes, id, quantity);
                else
                    Transfer(theater, outpost, changes, outpostChanges, id, quantity);
            }

            changed = true;
        }

        if (body["theaterToOutpostItems"] is JsonArray theaterToOutpost &&
            body["outpostToTheaterItems"] is JsonArray outpostToTheater)
        {
            multi ??= new JsonObject
            {
                ["profileRevision"] = outpost.GetLong("rvn"),
                ["profileId"] = "outpost0",
                ["profileChangesBaseRevision"] = outpost.GetLong("rvn"),
                ["profileChanges"] = new JsonArray(),
                ["profileCommandRevision"] = outpost.GetLong("commandRevision")
            };
            multiUpdate.Add(multi);
            var outpostChanges = (JsonArray)multi["profileChanges"]!;

            foreach (var itemNode in theaterToOutpost)
            {
                if (itemNode is not JsonObject entry) continue;
                var id = entry.GetStr("itemId");
                if (string.IsNullOrEmpty(id)) continue;
                Transfer(theater, outpost, changes, outpostChanges, id, entry.GetLong("quantity"));
            }

            foreach (var itemNode in outpostToTheater)
            {
                if (itemNode is not JsonObject entry) continue;
                var id = entry.GetStr("itemId");
                if (string.IsNullOrEmpty(id)) continue;
                Transfer(outpost, theater, outpostChanges, changes, id, entry.GetLong("quantity"));
            }

            changed = true;
        }

        if (changed)
        {
            theater["rvn"] = theater.GetLong("rvn") + 1;
            theater["commandRevision"] = theater.GetLong("commandRevision") + 1;
            outpost["rvn"] = outpost.GetLong("rvn") + 1;
            outpost["commandRevision"] = outpost.GetLong("commandRevision") + 1;

            var first = (JsonObject)multiUpdate[0]!;
            first["profileRevision"] = outpost.GetLong("rvn");
            first["profileCommandRevision"] = outpost.GetLong("commandRevision");

            McpCommon.Save("theater0", theater);
            McpCommon.Save("outpost0", outpost);
        }

        await McpCommon.Finish(ctx, "theater0", theater, baseRevision, changes, multiUpdate: multiUpdate);
    }

    private static void Transfer(JsonObject source, JsonObject target,
        JsonArray sourceChanges, JsonArray targetChanges, string id, long quantity)
    {
        var sourceItems = McpCommon.Items(source);
        var targetItems = McpCommon.Items(target);

        if (sourceItems[id] is not JsonObject sourceItem) return;

        var sourceQuantity = sourceItem.GetLong("quantity");

        if (targetItems[id] is JsonObject targetItem)
        {
            if (sourceQuantity > quantity)
            {
                targetItem["quantity"] = targetItem.GetLong("quantity") + quantity;
                sourceItem["quantity"] = sourceQuantity - quantity;

                sourceChanges.Add(McpCommon.ItemQuantityChanged(id, sourceItem.GetLong("quantity")));
                targetChanges.Add(McpCommon.ItemQuantityChanged(id, targetItem.GetLong("quantity")));
            }
            else
            {
                targetItem["quantity"] = targetItem.GetLong("quantity") + quantity;
                sourceItems.Remove(id);

                sourceChanges.Add(McpCommon.ItemQuantityChanged(id, sourceItem.GetLong("quantity")));
                targetChanges.Add(McpCommon.ItemRemoved(id));
            }
            return;
        }

        var item = (JsonObject)sourceItem.DeepClone();
        if (sourceQuantity > quantity)
        {
            sourceItem["quantity"] = sourceQuantity - quantity;
            item["quantity"] = quantity;
            targetItems[id] = item;

            targetChanges.Add(McpCommon.ItemAdded(id, item));
            sourceChanges.Add(McpCommon.ItemQuantityChanged(id, sourceItem.GetLong("quantity")));
        }
        else
        {
            targetItems[id] = item;
            sourceItems.Remove(id);

            targetChanges.Add(McpCommon.ItemAdded(id, item));
            sourceChanges.Add(McpCommon.ItemRemoved(id));
        }
    }

    public static async Task RecycleItemBatch(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var memory = Functions.GetVersionInfo(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var itemExists = false;

        if (body["targetItemIds"] is JsonArray targetIds)
        {
            foreach (var idNode in targetIds)
            {
                var id = J.AsString(idNode);
                if (string.IsNullOrEmpty(id)) continue;
                var item = McpCommon.Item(profile, id);
                if (item == null) continue;

                if (memory.Season > 11 || memory.Build == 11.30 || memory.Build == 11.31 ||
                    memory.Build == 11.40 || memory.Build == 11.50)
                {
                    var bookId = (item.GetStr("templateId") ?? "").ToLowerInvariant().StartsWith("schematic:")
                        ? "collection_book_schematics0"
                        : "collection_book_people0";
                    var book = McpCommon.Profile(bookId);

                    if (multiUpdate.Count == 0)
                    {
                        multiUpdate.Add(new JsonObject
                        {
                            ["profileRevision"] = book.GetLong("rvn"),
                            ["profileId"] = book.GetStr("profileId", bookId),
                            ["profileChangesBaseRevision"] = book.GetLong("rvn"),
                            ["profileChanges"] = new JsonArray(),
                            ["profileCommandRevision"] = book.GetLong("commandRevision")
                        });
                    }

                    var bookChanges = (JsonArray)((JsonObject)multiUpdate[0]!)["profileChanges"]!;
                    var bookItems = McpCommon.Items(book);
                    var template1 = item.GetStr("templateId") ?? "";

                    foreach (var key in bookItems.Select(kv => kv.Key).ToList())
                    {
                        var bookItem = bookItems[key] as JsonObject;
                        var template2 = bookItem?.GetStr("templateId") ?? "";
                        if (Prefix(template1) != Prefix(template2)) continue;

                        var remove = false;
                        if (template1.ToLowerInvariant().StartsWith("worker:") &&
                            template2.ToLowerInvariant().StartsWith("worker:"))
                        {
                            var p1 = (item["attributes"] as JsonObject)?.GetStr("personality");
                            var p2 = (bookItem?["attributes"] as JsonObject)?.GetStr("personality");
                            if (p1 != null && p2 != null && p1.ToLowerInvariant() == p2.ToLowerInvariant())
                            {
                                remove = (item["attributes"] as JsonObject)?.GetLong("level") >
                                         (bookItem?["attributes"] as JsonObject)?.GetLong("level");
                                itemExists = !remove;
                            }
                        }
                        else
                        {
                            remove = (item["attributes"] as JsonObject)?.GetLong("level") >
                                     (bookItem?["attributes"] as JsonObject)?.GetLong("level");
                            itemExists = !remove;
                        }

                        if (remove)
                        {
                            bookItems.Remove(key);
                            bookChanges.Add(McpCommon.ItemRemoved(key));
                            itemExists = false;
                        }
                    }

                    if (!itemExists)
                    {
                        bookItems[id] = item.DeepClone();
                        bookChanges.Add(McpCommon.ItemAdded(id, bookItems[id]));

                        notifications.Add(new JsonObject
                        {
                            ["type"] = "slotItemResult",
                            ["primary"] = true,
                            ["slottedItemId"] = id
                        });
                    }

                    McpCommon.Items(profile).Remove(id);
                    changes.Add(McpCommon.ItemRemoved(id));

                    McpCommon.Increment(book);
                    var update = (JsonObject)multiUpdate[0]!;
                    update["profileRevision"] = book.GetLong("rvn");
                    update["profileCommandRevision"] = book.GetLong("commandRevision");

                    McpCommon.Save(bookId, book);
                }
                else
                {
                    McpCommon.Items(profile).Remove(id);
                    changes.Add(McpCommon.ItemRemoved(id));
                }
            }

            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            multiUpdate: multiUpdate, notifications: notifications);
    }

    public static async Task ActivateConsumable(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        string xpBoost = null;

        var targetItemId = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(targetItemId))
        {
            var items = McpCommon.Items(profile);
            if (items[targetItemId] is JsonObject target)
                target["quantity"] = target.GetLong("quantity") - 1;

            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var item = items[key] as JsonObject;
                if (item?.GetStr("templateId") != "Token:xpboost") continue;

                var randomNumber = Random.Shared.Next(1250000);
                if (randomNumber < 1000000) randomNumber += 1000000;

                item["quantity"] = item.GetLong("quantity") + randomNumber;
                xpBoost = key;
            }

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            if (McpCommon.Item(profile, targetItemId!) is JsonObject target)
                changes.Add(McpCommon.ItemQuantityChanged(targetItemId!, target.GetLong("quantity")));

            if (xpBoost != null && McpCommon.Item(profile, xpBoost) is JsonObject boost)
                changes.Add(McpCommon.ItemQuantityChanged(xpBoost, boost.GetLong("quantity")));

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    private static string Prefix(string templateId)
    {
        var length = Math.Max(0, templateId.Length - 4);
        return templateId.Substring(0, length).ToLowerInvariant();
    }
}
