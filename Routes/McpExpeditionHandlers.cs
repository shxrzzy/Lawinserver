using System.Globalization;
using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpExpeditionHandlers
{
    public static async Task RefreshExpeditions(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var expeditionData = DataStore.Response("Campaign/expeditionData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var slots = new List<string>();
        var dateText = Functions.IsoNow();
        var date = ParseDate(dateText);

        var unlockQuests = expeditionData["questsUnlockingSlots"] as JsonArray;
        var slotsFromQuests = expeditionData["slotsFromQuests"] as JsonObject;
        var items = McpCommon.Items(profile);

        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            var item = items[key] as JsonObject;
            var templateId = (item?.GetStr("templateId") ?? "").ToLowerInvariant();
            if (unlockQuests == null || !unlockQuests.Any(n => J.AsString(n) == templateId)) continue;

            if ((item?["attributes"] as JsonObject)?.GetStr("quest_state") == "Claimed")
            {
                if (slotsFromQuests?[templateId] is JsonArray unlocked)
                    slots.AddRange(unlocked.Select(n => J.AsString(n) ?? ""));
            }
        }

        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            var item = items[key] as JsonObject;
            if (!(item?.GetStr("templateId") ?? "").ToLowerInvariant().StartsWith("expedition:")) continue;

            var attrs = item!["attributes"] as JsonObject ?? new JsonObject();
            var expirationEnd = ParseDate(attrs.GetStr("expedition_expiration_end_time") ?? dateText);

            if (date > expirationEnd && !attrs.ContainsKey("expedition_start_time"))
            {
                items.Remove(key);
                changes.Add(McpCommon.ItemRemoved(key));
                changed = true;
            }
            else
            {
                var slot = attrs.GetStr("expedition_slot_id");
                var index = slots.IndexOf(slot ?? "");
                if (index != -1) slots.RemoveAt(index);
            }
        }

        foreach (var slot in slots)
        {
            var item = CreateExpedition(expeditionData, slot, date);
            if (item == null) continue;
            var id = Functions.MakeId();
            items[id] = item;
            changes.Add(McpCommon.ItemAdded(id, item));
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task StartExpedition(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var memory = Functions.GetVersionInfo(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var expeditionData = DataStore.Response("Campaign/expeditionData.json") as JsonObject ?? new JsonObject();
        var itemRatingData = DataStore.Response("Campaign/itemRatingData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var dateText = Functions.IsoNow();
        var date = ParseDate(dateText);

        var expeditionId = body.GetStr("expeditionId");
        var squadId = body.GetStr("squadId");
        var itemIds = body["itemIds"] as JsonArray;
        var slotIndices = body["slotIndices"] as JsonArray;

        if (!string.IsNullOrEmpty(expeditionId) && !string.IsNullOrEmpty(squadId) &&
            itemIds != null && slotIndices != null)
        {
            var expedition = McpCommon.Item(profile, expeditionId);
            var expeditionAttrs = expedition?["attributes"] as JsonObject ?? new JsonObject();
            var expeditionLevel = expeditionAttrs.GetDouble("expedition_max_target_power");

            var rating = memory.Build < 13.20 ? itemRatingData["old"] as JsonObject : itemRatingData["new"] as JsonObject;
            rating ??= new JsonObject();

            var heroes = new List<JsonObject>();
            var items = McpCommon.Items(profile);

            foreach (var heroNode in itemIds)
            {
                var hero = J.AsString(heroNode);
                if (hero == null || items[hero] is not JsonObject heroItem) continue;

                var templateId = heroItem.GetStr("templateId") ?? "";
                var split = templateId.Split('_');
                var level = heroItem["attributes"]?["level"]?.DeepClone() ?? 1;
                var levelLong = J.AsLong(level);
                var lastTwo = split.Length >= 2
                    ? string.Join("_", split.Skip(split.Length - 2))
                    : (split.Length == 1 ? split[0] : "");
                var ratingKey = "_" + lastTwo.ToUpperInvariant();

                var powerLevel = 0.0;
                if (rating[ratingKey] is JsonObject ratingEntry)
                    powerLevel = ratingEntry.GetDouble(levelLong.ToString());

                heroes.Add(new JsonObject
                {
                    ["itemGuid"] = hero,
                    ["templateId"] = templateId,
                    ["class"] = split.Length > 1 ? split[1].ToLowerInvariant() : "",
                    ["rarity"] = split.Length >= 2 ? split[^2].ToLowerInvariant() : "",
                    ["tier"] = split.Length >= 1 ? split[^1].ToLowerInvariant() : "",
                    ["level"] = level?.DeepClone(),
                    ["powerLevel"] = powerLevel,
                    ["contributedLevel"] = powerLevel
                });
            }

            heroes.Sort((a, b) => b.GetDouble("powerLevel").CompareTo(a.GetDouble("powerLevel")));

            if (expeditionAttrs["expedition_criteria"] is JsonArray criteria)
            {
                var criteriaRequirements = expeditionData["criteriaRequirements"] as JsonObject;
                foreach (var criterionNode in criteria)
                {
                    var criterion = J.AsString(criterionNode) ?? "";
                    var requirements = criteriaRequirements?[criterion]?["requirements"] as JsonObject;
                    if (requirements == null) continue;

                    foreach (var hero in heroes)
                    {
                        var matching = true;
                        if (requirements.GetStr("class") != hero.GetStr("class")) matching = false;

                        if (requirements.ContainsKey("rarity"))
                        {
                            var rarityOk = false;
                            var heroRarity = hero.GetStr("rarity") ?? "";
                            if (requirements["rarity"] is JsonArray rarities)
                                rarityOk = rarities.Any(n => J.AsString(n) == heroRarity);
                            else
                                rarityOk = requirements.GetStr("rarity") == heroRarity;

                            if (!rarityOk) matching = false;
                        }

                        if (matching)
                        {
                            var modValue = criteriaRequirements![criterion]!.GetDouble("ModValue", 1);
                            hero["contributedLevel"] = hero.GetDouble("contributedLevel") +
                                                       hero.GetDouble("powerLevel") * (modValue - 1);
                            break;
                        }
                    }
                }
            }

            var totalPower = heroes.Sum(h => h.GetDouble("contributedLevel"));
            var successChance = expeditionLevel == 0 ? 0 : totalPower / expeditionLevel;
            if (successChance > 1) successChance = 1;

            for (var i = 0; i < itemIds.Count; i++)
            {
                var heroId = J.AsString(itemIds[i]);
                if (heroId == null || items[heroId] is not JsonObject heroItem) continue;

                var attrs = ItemAttrs(heroItem);
                attrs["squad_id"] = squadId.ToLowerInvariant();
                attrs["squad_slot_idx"] = i < slotIndices.Count ? slotIndices[i]?.DeepClone() : null;

                changes.Add(McpCommon.ItemAttrChanged(heroId, "squad_id", attrs["squad_id"]));
                changes.Add(McpCommon.ItemAttrChanged(heroId, "squad_slot_idx", attrs["squad_slot_idx"]));
            }

            var templateId2 = expedition?.GetStr("templateId") ?? "";
            var duration = (expeditionData["attributes"] as JsonObject)?[templateId2]?.GetDouble("expedition_duration_minutes") ?? 0;
            var endDate = Functions.Iso(date.AddMinutes(duration).UtcDateTime);

            expeditionAttrs["expedition_squad_id"] = squadId.ToLowerInvariant();
            expeditionAttrs["expedition_success_chance"] = successChance;
            expeditionAttrs["expedition_start_time"] = dateText;
            expeditionAttrs["expedition_end_time"] = endDate;

            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_squad_id", expeditionAttrs["expedition_squad_id"]));
            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_success_chance", expeditionAttrs["expedition_success_chance"]));
            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_start_time", expeditionAttrs["expedition_start_time"]));
            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_end_time", expeditionAttrs["expedition_end_time"]));

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task AbandonExpedition(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var expeditionData = DataStore.Response("Campaign/expeditionData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var dateText = Functions.IsoNow();
        var date = ParseDate(dateText);

        var expeditionId = body.GetStr("expeditionId");
        if (!string.IsNullOrEmpty(expeditionId))
        {
            var expedition = McpCommon.Item(profile, expeditionId);
            var attrs = expedition?["attributes"] as JsonObject ?? new JsonObject();
            var squadId = attrs.GetStr("expedition_squad_id");

            var items = McpCommon.Items(profile);
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var heroAttrs = (items[key] as JsonObject)?["attributes"] as JsonObject;
                if (heroAttrs == null || !heroAttrs.ContainsKey("squad_id")) continue;
                if (heroAttrs.GetStr("squad_id") != squadId) continue;

                heroAttrs["squad_id"] = "";
                heroAttrs["squad_slot_idx"] = -1;
                changes.Add(McpCommon.ItemAttrChanged(key, "squad_id", heroAttrs["squad_id"]));
                changes.Add(McpCommon.ItemAttrChanged(key, "squad_slot_idx", heroAttrs["squad_slot_idx"]));
            }

            attrs.Remove("expedition_squad_id");
            attrs.Remove("expedition_start_time");
            attrs.Remove("expedition_end_time");

            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_squad_id", ""));
            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_start_time", null));
            changes.Add(McpCommon.ItemAttrChanged(expeditionId, "expedition_end_time", null));

            var expirationEnd = ParseDate(attrs.GetStr("expedition_expiration_end_time") ?? dateText);
            if (date > expirationEnd)
            {
                var slot = attrs.GetStr("expedition_slot_id") ?? "";
                items.Remove(expeditionId);
                changes.Add(McpCommon.ItemRemoved(expeditionId));

                var newItem = CreateExpedition(expeditionData, slot, date);
                if (newItem != null)
                {
                    var id = Functions.MakeId();
                    items[id] = newItem;
                    changes.Add(McpCommon.ItemAdded(id, newItem));
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

    public static async Task CollectExpedition(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var queryProfileId = RequestUtil.Query(ctx, "profileId");
        var profile = McpCommon.Profile(profileId);
        var expeditionData = DataStore.Response("Campaign/expeditionData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var otherProfiles = new List<(string Id, JsonObject Profile, JsonObject Update)>();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var dateText = Functions.IsoNow();
        var date = ParseDate(dateText);

        var expeditionId = body.GetStr("expeditionId");
        if (!string.IsNullOrEmpty(expeditionId))
        {
            var result = new JsonObject
            {
                ["type"] = "expeditionResult",
                ["primary"] = true,
                ["client_request_id"] = "",
                ["bExpeditionSucceeded"] = false
            };
            notifications.Add(result);

            var expedition = McpCommon.Item(profile, expeditionId);
            var attrs = expedition?["attributes"] as JsonObject ?? new JsonObject();
            var successChance = attrs.GetDouble("expedition_success_chance");

            if (Random.Shared.NextDouble() < successChance)
            {
                result["bExpeditionSucceeded"] = true;
                var rewards = new JsonArray();
                result["expeditionRewards"] = rewards;

                if (expeditionData["rewards"] is JsonArray rewardGroups)
                {
                    foreach (var groupNode in rewardGroups)
                    {
                        if (groupNode is not JsonArray group || group.Count == 0) continue;
                        var reward = group[Random.Shared.Next(group.Count)] as JsonObject;
                        if (reward == null) continue;

                        var id = Functions.MakeId();
                        var templateId = reward.GetStr("templateId") ?? "";
                        var itemProfile = reward.GetStr("itemProfile") ?? "";
                        var minQ = reward.GetLong("minQuantity");
                        var maxQ = reward.GetLong("maxQuantity");
                        var quantity = Random.Shared.Next((int)(maxQ - minQ + 1)) + minQ;

                        var item = McpCommon.NewItem(templateId, new JsonObject
                        {
                            ["loadedAmmo"] = 0,
                            ["inventory_overflow_date"] = false,
                            ["level"] = 0,
                            ["alterationDefinitions"] = new JsonArray(),
                            ["durability"] = 1,
                            ["itemSource"] = ""
                        }, quantity);

                        rewards.Add(new JsonObject
                        {
                            ["itemType"] = templateId,
                            ["itemGuid"] = id,
                            ["itemProfile"] = itemProfile,
                            ["quantity"] = quantity
                        });

                        if (itemProfile == queryProfileId)
                        {
                            McpCommon.Items(profile)[id] = item;
                            changes.Add(McpCommon.ItemAdded(id, item));
                        }
                        else
                        {
                            var index = otherProfiles.FindIndex(p => p.Id == itemProfile);
                            if (index == -1)
                            {
                                var other = McpCommon.Profile(itemProfile);
                                var update = new JsonObject
                                {
                                    ["profileRevision"] = other.GetLong("rvn"),
                                    ["profileId"] = other.GetStr("profileId", itemProfile),
                                    ["profileChangesBaseRevision"] = other.GetLong("rvn"),
                                    ["profileChanges"] = new JsonArray(),
                                    ["profileCommandRevision"] = other.GetLong("commandRevision")
                                };
                                otherProfiles.Add((itemProfile, other, update));
                                multiUpdate.Add(update);
                                index = otherProfiles.Count - 1;
                            }

                            McpCommon.Items(otherProfiles[index].Profile)[id] = item;
                            ((JsonArray)otherProfiles[index].Update["profileChanges"]!).Add(McpCommon.ItemAdded(id, item));
                        }
                    }
                }
            }

            var squadId = attrs.GetStr("expedition_squad_id");
            var items = McpCommon.Items(profile);
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var heroAttrs = (items[key] as JsonObject)?["attributes"] as JsonObject;
                if (heroAttrs == null || !heroAttrs.ContainsKey("squad_id")) continue;
                if (heroAttrs.GetStr("squad_id") != squadId) continue;

                heroAttrs["squad_id"] = "";
                heroAttrs["squad_slot_idx"] = -1;
                changes.Add(McpCommon.ItemAttrChanged(key, "squad_id", heroAttrs["squad_id"]));
                changes.Add(McpCommon.ItemAttrChanged(key, "squad_slot_idx", heroAttrs["squad_slot_idx"]));
            }

            var slot = attrs.GetStr("expedition_slot_id") ?? "";
            items.Remove(expeditionId);
            changes.Add(McpCommon.ItemRemoved(expeditionId));

            var newItem = CreateExpedition(expeditionData, slot, date);
            if (newItem != null)
            {
                var id = Functions.MakeId();
                items[id] = newItem;
                changes.Add(McpCommon.ItemAdded(id, newItem));
            }

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);

            foreach (var other in otherProfiles)
            {
                McpCommon.Increment(other.Profile);
                other.Update["profileRevision"] = other.Profile.GetLong("rvn");
                other.Update["profileCommandRevision"] = other.Profile.GetLong("commandRevision");
                McpCommon.Save(other.Id, other.Profile);
            }
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            multiUpdate: multiUpdate, notifications: notifications);
    }

    public static async Task OpenCardPack(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var cardPackData = DataStore.Response("Campaign/cardPackData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var cardPackItemId = body.GetStr("cardPackItemId");
        if (!string.IsNullOrEmpty(cardPackItemId))
        {
            var pack = McpCommon.Item(profile, cardPackItemId);
            var packTemplate = pack?.GetStr("templateId") ?? "";
            var split = packTemplate.Split(':');

            var result = new JsonObject
            {
                ["type"] = "cardPackResult",
                ["primary"] = true,
                ["lootGranted"] = new JsonObject
                {
                    ["tierGroupName"] = split.Length > 1 ? split[1] : "",
                    ["items"] = new JsonArray()
                },
                ["displayLevel"] = 0
            };
            notifications.Add(result);
            var lootItems = (JsonArray)((JsonObject)result["lootGranted"]!)["items"]!;

            var choicePacks = cardPackData["choiceCardPacks"] as JsonArray;
            var isChoice = choicePacks != null && choicePacks.Any(n => J.AsString(n) == packTemplate);

            if (isChoice)
            {
                var options = pack?["attributes"]?["options"] as JsonArray;
                var selectionIdx = (int)body.GetLong("selectionIdx");
                var chosen = options != null && selectionIdx >= 0 && selectionIdx < options.Count
                    ? options[selectionIdx] as JsonObject
                    : null;

                if (chosen != null)
                {
                    var id = Functions.MakeId();
                    var item = new JsonObject
                    {
                        ["templateId"] = chosen["itemType"]?.DeepClone(),
                        ["attributes"] = chosen["attributes"]?.DeepClone(),
                        ["quantity"] = chosen["quantity"]?.DeepClone()
                    };

                    McpCommon.Items(profile)[id] = item;
                    changes.Add(McpCommon.ItemAdded(id, item));

                    lootItems.Add(new JsonObject
                    {
                        ["itemType"] = item["templateId"]?.DeepClone(),
                        ["itemGuid"] = id,
                        ["itemProfile"] = RequestUtil.Query(ctx, "profileId"),
                        ["attributes"] = item["attributes"]?.DeepClone(),
                        ["quantity"] = item["quantity"]?.DeepClone()
                    });
                }
            }
            else
            {
                for (var i = 0; i < 10; i++)
                {
                    var id = Functions.MakeId();
                    var (item, notificationType) = RollCardPackItem(cardPackData);

                    McpCommon.Items(profile)[id] = item;
                    changes.Add(McpCommon.ItemAdded(id, item));

                    lootItems.Add(new JsonObject
                    {
                        ["itemType"] = notificationType,
                        ["itemGuid"] = id,
                        ["itemProfile"] = RequestUtil.Query(ctx, "profileId"),
                        ["attributes"] = item["attributes"]?.DeepClone(),
                        ["quantity"] = 1
                    });
                }
            }

            var items = McpCommon.Items(profile);
            if (pack != null && pack.GetLong("quantity") <= 1)
            {
                items.Remove(cardPackItemId);
                changes.Add(McpCommon.ItemRemoved(cardPackItemId));
            }
            else if (items[cardPackItemId] is JsonObject remainingPack)
            {
                remainingPack["quantity"] = remainingPack.GetLong("quantity") - 1;
                changes.Add(McpCommon.ItemQuantityChanged(cardPackItemId, remainingPack.GetLong("quantity")));
            }

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        
        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            notifications: notifications, applyFallback: false);
    }

    public static async Task PopulatePrerolledOffers(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var cardPackData = DataStore.Response("Campaign/cardPackData.json") as JsonObject ?? new JsonObject();
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var dateText = Functions.IsoNow();
        var date = ParseDate(dateText);

        var items = McpCommon.Items(profile);
        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            var item = items[key] as JsonObject;
            if ((item?.GetStr("templateId") ?? "").ToLowerInvariant() != "prerolldata:preroll_basic") continue;

            var attrs = item!["attributes"] as JsonObject ?? new JsonObject();
            var expiration = ParseDate(attrs.GetStr("expiration") ?? dateText);
            if (date <= expiration) continue;

            var prerollItems = new JsonArray();
            attrs["items"] = prerollItems;

            for (var i = 0; i < 10; i++)
            {
                var (profileItem, _) = RollCardPackItem(cardPackData);
                var prerollItem = new JsonObject
                {
                    ["itemType"] = profileItem["templateId"]?.DeepClone(),
                    ["attributes"] = profileItem["attributes"]?.DeepClone(),
                    ["quantity"] = profileItem["quantity"]?.DeepClone()
                };
                prerollItems.Add(prerollItem);
            }

            changes.Add(McpCommon.ItemAttrChanged(key, "items", attrs["items"]));

            attrs["expiration"] = Functions.IsoNow().Split('T')[0] + "T23:59:59.999Z";
            changes.Add(McpCommon.ItemAttrChanged(key, "expiration", attrs["expiration"]));

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    private static (JsonObject Item, string NotificationType) RollCardPackItem(JsonObject cardPackData)
    {
        var itemIds = cardPackData["default"] as JsonArray ?? new JsonArray();
        var random = itemIds.Count == 0 ? 0 : Random.Shared.Next(itemIds.Count);
        var templateId = itemIds.Count > 0 ? J.AsString(itemIds[random]) ?? "" : "";

        var item = McpCommon.NewItem(templateId, LootAttributes());
        var notificationType = templateId;

        if (Random.Shared.NextDouble() < 0.1 && cardPackData["choiceCardPacks"] is JsonArray choicePacks && choicePacks.Count > 0)
        {
            var cpTemplateId = J.AsString(choicePacks[Random.Shared.Next(choicePacks.Count)]) ?? "";
            var cpItem = new JsonObject
            {
                ["templateId"] = cpTemplateId,
                ["attributes"] = new JsonObject
                {
                    ["level"] = 1,
                    ["pack_source"] = "Store",
                    ["options"] = new JsonArray()
                },
                ["quantity"] = 1
            };

            itemIds = cardPackData[cpTemplateId.ToLowerInvariant()] as JsonArray
                      ?? cardPackData["default"] as JsonArray
                      ?? new JsonArray();

            var options = (JsonArray)cpItem["attributes"]!["options"]!;
            for (var x = 0; x < 2; x++)
            {
                random = itemIds.Count == 0 ? 0 : Random.Shared.Next(itemIds.Count);
                var subTemplate = itemIds.Count > 0 ? J.AsString(itemIds[random]) ?? "" : "";

                var subItem = new JsonObject
                {
                    ["itemType"] = subTemplate,
                    ["attributes"] = LootAttributes(),
                    ["quantity"] = 1
                };

                if (subTemplate.ToLowerInvariant().StartsWith("worker:"))
                    subItem["attributes"] = Functions.MakeSurvivorAttributes(subTemplate);

                var removeIndex = -1;
                for (var j = 0; j < itemIds.Count; j++)
                {
                    if (J.AsString(itemIds[j]) == subTemplate) { removeIndex = j; break; }
                }
                if (removeIndex >= 0) itemIds.RemoveAt(removeIndex);

                options.Add(subItem);
            }

            item = cpItem;
            notificationType = random < itemIds.Count ? J.AsString(itemIds[random]) : null;
        }

        return (item, notificationType);
    }

    private static JsonObject LootAttributes() => new()
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

    private static JsonObject CreateExpedition(JsonObject expeditionData, string slot, DateTimeOffset date)
    {
        var slotData = (expeditionData["slots"] as JsonObject)?[slot] as JsonObject;
        if (slotData == null) return null;

        JsonArray options;
        if (slotData.ContainsKey("rare") && Random.Shared.NextDouble() < 0.05)
            options = slotData["rare"] as JsonArray;
        else
            options = slotData["normal"] as JsonArray;

        if (options == null || options.Count == 0) return null;

        var templateId = J.AsString(options[Random.Shared.Next(options.Count)]) ?? "";
        var attrsData = (expeditionData["attributes"] as JsonObject)?[templateId] as JsonObject;
        var endDate = Functions.Iso(date.AddMinutes(attrsData?.GetDouble("expiration_duration_minutes") ?? 0).UtcDateTime);

        var item = McpCommon.NewItem(templateId, new JsonObject
        {
            ["expedition_expiration_end_time"] = endDate,
            ["expedition_criteria"] = new JsonArray(),
            ["level"] = 1,
            ["expedition_max_target_power"] = attrsData?["expedition_max_target_power"]?.DeepClone(),
            ["expedition_min_target_power"] = attrsData?["expedition_min_target_power"]?.DeepClone(),
            ["expedition_slot_id"] = slot,
            ["expedition_expiration_start_time"] = Functions.Iso(date.UtcDateTime)
        });

        var criteria = item["attributes"]!["expedition_criteria"] as JsonArray;
        var criteriaPool = expeditionData["criteria"] as JsonArray;
        if (criteria != null && criteriaPool is { Count: > 0 })
        {
            for (var x = 0; x < 3; x++)
            {
                if (Random.Shared.NextDouble() < 0.2)
                    criteria.Add(criteriaPool[Random.Shared.Next(criteriaPool.Count)]?.DeepClone());
            }
        }

        return item;
    }

    private static DateTimeOffset ParseDate(string value)
    {
        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date))
            return date;
        return DateTimeOffset.UtcNow;
    }

    private static JsonObject ItemAttrs(JsonObject item)
    {
        if (item["attributes"] is not JsonObject attrs)
        {
            attrs = new JsonObject();
            item["attributes"] = attrs;
        }
        return attrs;
    }
}
