using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpQuestHandlers
{
    public static async Task FortRerollDailyQuest(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var queryProfileId = RequestUtil.Query(ctx, "profileId");
        var dailyPath = queryProfileId == "profile0" || queryProfileId == "campaign"
            ? "Campaign/quests.json"
            : "Athena/quests.json";
        var daily = (DataStore.Response(dailyPath) as JsonObject)?["Daily"] as JsonArray ?? new JsonArray();

        var newQuestId = Functions.MakeId();
        var randomNumber = daily.Count == 0 ? 0 : Random.Shared.Next(daily.Count);

        var items = McpCommon.Items(profile);
        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            var existingTemplate = (items[key] as JsonObject)?.GetStr("templateId") ?? "";
            var guard = 0;
            while (daily.Count > 0 &&
                   (daily[randomNumber] as JsonObject)?.GetStr("templateId")?.ToLowerInvariant() == existingTemplate.ToLowerInvariant())
            {
                randomNumber = Random.Shared.Next(daily.Count);
                if (++guard > 1000) break;
            }
        }

        var questId = body.GetStr("questId");
        var questManager = McpCommon.Attrs(profile)["quest_manager"] as JsonObject;

        if (!string.IsNullOrEmpty(questId) && questManager != null && questManager.GetLong("dailyQuestRerolls") >= 1)
        {
            questManager["dailyQuestRerolls"] = questManager.GetLong("dailyQuestRerolls") - 1;

            items.Remove(questId);

            var template = daily.Count > 0 ? (daily[randomNumber] as JsonObject) : null;
            if (template != null)
            {
                var item = McpCommon.NewItem(template.GetStr("templateId") ?? "", new JsonObject
                {
                    ["creation_time"] = Functions.IsoNow(),
                    ["level"] = -1,
                    ["item_seen"] = false,
                    ["sent_new_notification"] = false,
                    ["xp_reward_scalar"] = 1,
                    ["quest_state"] = "Active",
                    ["last_state_change_time"] = Functions.IsoNow(),
                    ["max_level_bonus"] = 0,
                    ["xp"] = 0,
                    ["favorite"] = false
                });

                var attrs = (JsonObject)item["attributes"]!;
                if (template["objectives"] is JsonArray objectives)
                {
                    foreach (var objective in objectives)
                    {
                        var stat = J.AsString(objective) ?? "";
                        attrs[$"completion_{stat.ToLowerInvariant()}"] = 0;
                    }
                }

                items[newQuestId] = item;

                changed = true;

                changes.Add(McpCommon.StatModified("quest_manager", questManager));
                changes.Add(McpCommon.ItemAdded(newQuestId, item));
                changes.Add(McpCommon.ItemRemoved(questId));

                notifications.Add(new JsonObject
                {
                    ["type"] = "dailyQuestReroll",
                    ["primary"] = true,
                    ["newQuestId"] = template.GetStr("templateId")
                });
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task ClientQuestLogin(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var athenaQuests = (DataStore.Response("Athena/quests.json") as JsonObject)?.DeepClone() as JsonObject
                           ?? new JsonObject();
        var campaignQuests = (DataStore.Response("Campaign/quests.json") as JsonObject)?.DeepClone() as JsonObject
                             ?? new JsonObject();
        var memory = Functions.GetVersionInfo(ctx);

        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var questCount = 0;
        var shouldGiveQuest = true;
        var dateFormat = Functions.IsoNow().Split('T')[0];
        JsonArray dailyQuestIds = null;
        JsonObject seasonQuestIds = null;

        var seasonPrefix = memory.Season < 10 ? $"0{memory.Season}" : memory.Season.ToString();
        var queryProfileId = RequestUtil.Query(ctx, "profileId");
        var grantFounders = AppConfig.GetBool("Profile", "bGrantFoundersPacks", false);
        var completeSeasonal = AppConfig.GetBool("Profile", "bCompletedSeasonalQuests", false);

        var items = McpCommon.Items(profile);

        try
        {
            if (queryProfileId == "profile0" || queryProfileId == "campaign")
            {
                dailyQuestIds = campaignQuests["Daily"] as JsonArray;

                if (campaignQuests.ContainsKey($"Season{seasonPrefix}"))
                    seasonQuestIds = campaignQuests[$"Season{seasonPrefix}"] as JsonObject;

                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var template = (items[key] as JsonObject)?.GetStr("templateId") ?? "";
                    if (template.ToLowerInvariant().StartsWith("quest:daily")) questCount += 1;
                }

                if (grantFounders)
                {
                    var questsToGrant = new[]
                    {
                        "Quest:foundersquest_getrewards_0_1",
                        "Quest:foundersquest_getrewards_1_2",
                        "Quest:foundersquest_getrewards_2_3",
                        "Quest:foundersquest_getrewards_3_4",
                        "Quest:foundersquest_chooseherobundle",
                        "Quest:foundersquest_getrewards_4_5",
                        "Quest:foundersquest_herobundle_nochoice"
                    };

                    foreach (var grant in questsToGrant)
                    {
                        var skip = false;
                        foreach (var key in items.Select(kv => kv.Key).ToList())
                        {
                            if ((items[key] as JsonObject)?.GetStr("templateId")?.ToLowerInvariant() ==
                                grant.ToLowerInvariant())
                            {
                                skip = true;
                            }
                        }

                        if (skip) continue;

                        var itemId = Functions.MakeId();
                        var item = McpCommon.NewItem(grant, new JsonObject
                        {
                            ["creation_time"] = "min",
                            ["quest_state"] = "Completed",
                            ["last_state_change_time"] = Functions.IsoNow(),
                            ["level"] = -1,
                            ["sent_new_notification"] = true,
                            ["xp_reward_scalar"] = 1
                        });

                        items[itemId] = item;
                        changes.Add(McpCommon.ItemAdded(itemId, item));
                        changed = true;
                    }
                }
            }

            if (queryProfileId == "athena")
            {
                dailyQuestIds = athenaQuests["Daily"] as JsonArray;

                if (athenaQuests.ContainsKey($"Season{seasonPrefix}"))
                    seasonQuestIds = athenaQuests[$"Season{seasonPrefix}"] as JsonObject;

                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var template = (items[key] as JsonObject)?.GetStr("templateId") ?? "";
                    if (template.ToLowerInvariant().StartsWith("quest:athenadaily")) questCount += 1;
                }
            }

            var profileAttrs = McpCommon.Attrs(profile);
            var questManager = profileAttrs["quest_manager"] as JsonObject;
            if (questManager != null && questManager.ContainsKey("dailyLoginInterval"))
            {
                var interval = questManager.GetStr("dailyLoginInterval");
                if (interval != null && interval.Contains('T'))
                {
                    var dailyLoginDate = interval.Split('T')[0];

                    if (dailyLoginDate == dateFormat)
                    {
                        shouldGiveQuest = false;
                    }
                    else
                    {
                        shouldGiveQuest = true;
                        if (questManager.GetLong("dailyQuestRerolls") <= 0)
                            questManager["dailyQuestRerolls"] = questManager.GetLong("dailyQuestRerolls") + 1;
                    }
                }
            }

            if (questCount < 3 && shouldGiveQuest && dailyQuestIds is { Count: > 0 } && questManager != null)
            {
                var newQuestId = Functions.MakeId();
                var randomNumber = Random.Shared.Next(dailyQuestIds.Count);

                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var existingTemplate = (items[key] as JsonObject)?.GetStr("templateId") ?? "";
                    var guard = 0;
                    while ((dailyQuestIds[randomNumber] as JsonObject)?.GetStr("templateId")?.ToLowerInvariant() ==
                           existingTemplate.ToLowerInvariant())
                    {
                        randomNumber = Random.Shared.Next(dailyQuestIds.Count);
                        if (++guard > 1000) break;
                    }
                }

                var template = dailyQuestIds[randomNumber] as JsonObject;
                if (template != null)
                {
                    var item = McpCommon.NewItem(template.GetStr("templateId") ?? "", new JsonObject
                    {
                        ["creation_time"] = Functions.IsoNow(),
                        ["level"] = -1,
                        ["item_seen"] = false,
                        ["sent_new_notification"] = false,
                        ["xp_reward_scalar"] = 1,
                        ["quest_state"] = "Active",
                        ["last_state_change_time"] = Functions.IsoNow(),
                        ["max_level_bonus"] = 0,
                        ["xp"] = 0,
                        ["favorite"] = false
                    });

                    var attrs = (JsonObject)item["attributes"]!;
                    if (template["objectives"] is JsonArray objectives)
                    {
                        foreach (var objective in objectives)
                        {
                            var stat = J.AsString(objective) ?? "";
                            attrs[$"completion_{stat.ToLowerInvariant()}"] = 0;
                        }
                    }

                    items[newQuestId] = item;

                    questManager["dailyLoginInterval"] = Functions.IsoNow();

                    changes.Add(McpCommon.ItemAdded(newQuestId, item));
                    changes.Add(McpCommon.StatModified("quest_manager", questManager));

                    changed = true;
                }
            }
        }
        catch
        {
            
        }

        
        foreach (var key in items.Select(kv => kv.Key).ToList())
        {
            if (key.Length >= 5 && key.StartsWith("QS") &&
                char.IsDigit(key[2]) && char.IsDigit(key[3]) && key[4] == '-')
            {
                if (!key.StartsWith($"QS{seasonPrefix}-", StringComparison.Ordinal))
                {
                    items.Remove(key);
                    changes.Add(McpCommon.ItemRemoved(key));
                    changed = true;
                }
            }
        }

        if (seasonQuestIds != null)
        {
            var questsToAdd = new List<string>();

            if (queryProfileId == "athena")
            {
                if (seasonQuestIds["ChallengeBundleSchedules"] is JsonObject schedules)
                {
                    foreach (var pair in schedules.ToList())
                    {
                        var scheduleId = pair.Key;
                        if (items.ContainsKey(scheduleId))
                            changes.Add(McpCommon.ItemRemoved(scheduleId));

                        if (pair.Value is not JsonObject schedule) continue;

                        var item = McpCommon.NewItem(schedule.GetStr("templateId") ?? "", new JsonObject
                        {
                            ["unlock_epoch"] = Functions.IsoNow(),
                            ["max_level_bonus"] = 0,
                            ["level"] = 1,
                            ["item_seen"] = true,
                            ["xp"] = 0,
                            ["favorite"] = false,
                            ["granted_bundles"] = schedule["granted_bundles"]?.DeepClone()
                        });

                        items[scheduleId] = item;
                        changes.Add(McpCommon.ItemAdded(scheduleId, item));
                        changed = true;
                    }
                }

                if (seasonQuestIds["ChallengeBundles"] is JsonObject bundles)
                {
                    foreach (var pair in bundles.ToList())
                    {
                        var bundleId = pair.Key;
                        if (items.ContainsKey(bundleId))
                            changes.Add(McpCommon.ItemRemoved(bundleId));

                        if (pair.Value is not JsonObject bundle) continue;

                        if (completeSeasonal && bundle.ContainsKey("questStages"))
                        {
                            var granted = bundle["grantedquestinstanceids"] as JsonArray ?? new JsonArray();
                            if (bundle["questStages"] is JsonArray stages)
                            {
                                foreach (var stage in stages) granted.Add(stage?.DeepClone());
                            }
                            bundle["grantedquestinstanceids"] = granted;
                        }

                        var grantedIds = bundle["grantedquestinstanceids"] as JsonArray ?? new JsonArray();

                        var item = McpCommon.NewItem(bundle.GetStr("templateId") ?? "", new JsonObject
                        {
                            ["has_unlock_by_completion"] = false,
                            ["num_quests_completed"] = 0,
                            ["level"] = 0,
                            ["grantedquestinstanceids"] = grantedIds.DeepClone(),
                            ["item_seen"] = true,
                            ["max_allowed_bundle_level"] = 0,
                            ["num_granted_bundle_quests"] = 0,
                            ["max_level_bonus"] = 0,
                            ["challenge_bundle_schedule_id"] = bundle["challenge_bundle_schedule_id"]?.DeepClone(),
                            ["num_progress_quests_completed"] = 0,
                            ["xp"] = 0,
                            ["favorite"] = false
                        });

                        items[bundleId] = item;
                        questsToAdd.AddRange(grantedIds.Select(n => J.AsString(n) ?? ""));

                        var attrs = (JsonObject)item["attributes"]!;
                        attrs["num_granted_bundle_quests"] = grantedIds.Count;

                        if (completeSeasonal)
                        {
                            attrs["num_quests_completed"] = grantedIds.Count;
                            attrs["num_progress_quests_completed"] = grantedIds.Count;

                            var bundleTemplate = (bundle.GetStr("templateId") ?? "").ToLowerInvariant();
                            if ((memory.Season == 10 || memory.Season == 11) &&
                                (bundleTemplate.Contains("missionbundle_s10_0") ||
                                 bundleTemplate == "challengebundle:missionbundle_s11_stretchgoals2"))
                            {
                                attrs["level"] = attrs.GetLong("level") + 1;
                            }
                        }

                        changes.Add(McpCommon.ItemAdded(bundleId, item));
                        changed = true;
                    }
                }
            }
            else
            {
                
                if (seasonQuestIds["Quests"] is JsonObject quests)
                {
                    foreach (var pair in quests.ToList())
                        questsToAdd.Add(pair.Key);
                }
            }

            var inProgress = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            void ParseQuest(string questId)
            {
                if (!inProgress.Add(questId)) return;

                var quest = (seasonQuestIds["Quests"] as JsonObject)?[questId] as JsonObject;
                if (quest == null)
                {
                    inProgress.Remove(questId);
                    return;
                }

                if (items.ContainsKey(questId))
                    changes.Add(McpCommon.ItemRemoved(questId));

                var item = McpCommon.NewItem(quest.GetStr("templateId") ?? "", new JsonObject
                {
                    ["creation_time"] = Functions.IsoNow(),
                    ["level"] = -1,
                    ["item_seen"] = true,
                    ["sent_new_notification"] = true,
                    ["challenge_bundle_id"] = quest.GetStr("challenge_bundle_id") ?? "",
                    ["xp_reward_scalar"] = 1,
                    ["quest_state"] = "Active",
                    ["last_state_change_time"] = Functions.IsoNow(),
                    ["max_level_bonus"] = 0,
                    ["xp"] = 0,
                    ["favorite"] = false
                });

                items[questId] = item;
                var attrs = (JsonObject)item["attributes"]!;

                if (completeSeasonal)
                {
                    attrs["quest_state"] = "Claimed";

                    if (quest["rewards"] is JsonArray rewards)
                    {
                        foreach (var rewardNode in rewards)
                        {
                            if (rewardNode is not JsonObject reward) continue;
                            var rewardTemplate = reward.GetStr("templateId") ?? "";
                            if (!rewardTemplate.StartsWith("Quest:", StringComparison.Ordinal)) continue;

                            if (seasonQuestIds["Quests"] is JsonObject allQuests &&
                                seasonQuestIds["ChallengeBundles"] is JsonObject allBundles)
                            {
                                foreach (var pair in allQuests.ToList())
                                {
                                    if ((pair.Value as JsonObject)?.GetStr("templateId") != rewardTemplate) continue;

                                    var bundleKey = (pair.Value as JsonObject)?.GetStr("challenge_bundle_id");
                                    if (bundleKey != null && allBundles[bundleKey] is JsonObject bundle)
                                    {
                                        var granted = bundle["grantedquestinstanceids"] as JsonArray;
                                        if (granted == null)
                                        {
                                            granted = new JsonArray();
                                            bundle["grantedquestinstanceids"] = granted;
                                        }
                                        granted.Add(pair.Key);
                                    }

                                    ParseQuest(pair.Key);
                                }
                            }
                        }
                    }
                }

                if (quest["objectives"] is JsonObject objectives)
                {
                    foreach (var objective in objectives.ToList())
                    {
                        attrs[$"completion_{objective.Key}"] = completeSeasonal
                            ? objective.Value?.DeepClone()
                            : 0;
                    }
                }

                changes.Add(McpCommon.ItemAdded(questId, item));
                changed = true;

                inProgress.Remove(questId);
            }

            foreach (var questId in questsToAdd)
                ParseQuest(questId);
        }

        
        if (memory.Season >= 41)
        {
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var template = (items[key] as JsonObject)?.GetStr("templateId") ?? "";
                if (!template.ToLowerInvariant().StartsWith("athenaseason:")) continue;

                items.Remove(key);
                changes.Add(McpCommon.ItemRemoved(key));
            }

            var id = Functions.MakeId();
            var item = McpCommon.NewItem($"AthenaSeason:athenaseason{memory.Season}", new JsonObject
            {
                ["level"] = 100,
                ["currency_season_total"] = 100,
                ["purchase_date"] = "min",
                ["purchase_context"] = "None"
            });

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

    public static async Task UpdateQuestClientObjectives(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var items = McpCommon.Items(profile);

        if (body["advance"] is JsonArray advance)
        {
            for (var oi = 0; oi < advance.Count; oi++)
            {
                if (advance[oi] is not JsonObject outerAdvance) continue;
                var outerStatName = outerAdvance.GetStr("statName") ?? "";
                var questsToUpdate = new List<string>();

                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var item = items[key] as JsonObject;
                    if (!(item?.GetStr("templateId") ?? "").ToLowerInvariant().StartsWith("quest:")) continue;

                    if (item!["attributes"] is JsonObject attrs)
                    {
                        foreach (var attr in attrs.ToList())
                        {
                            if (attr.Key.ToLowerInvariant() == $"completion_{outerStatName}".ToLowerInvariant())
                                questsToUpdate.Add(key);
                        }
                    }
                }

                
                
                for (var ii = 0; ii < questsToUpdate.Count; ii++)
                {
                    var adv = ii < advance.Count ? advance[ii] as JsonObject : outerAdvance;
                    if (adv == null) continue;

                    var statName = adv.GetStr("statName") ?? "";
                    var count = adv["count"]?.DeepClone();
                    var quest = McpCommon.Item(profile, questsToUpdate[ii]);
                    var attrs = quest?["attributes"] as JsonObject;
                    if (attrs == null) continue;

                    attrs[$"completion_{statName}"] = count;

                    changes.Add(McpCommon.ItemAttrChanged(questsToUpdate[ii], $"completion_{statName}", count));

                    if ((attrs.GetStr("quest_state") ?? "").ToLowerInvariant() != "claimed")
                    {
                        var incomplete = false;

                        foreach (var attr in attrs.ToList())
                        {
                            if (!attr.Key.ToLowerInvariant().StartsWith("completion_")) continue;
                            if (J.AsDouble(attr.Value) == 0) incomplete = true;
                        }

                        if (!incomplete)
                        {
                            attrs["quest_state"] = "Claimed";
                            changes.Add(McpCommon.ItemAttrChanged(questsToUpdate[ii], "quest_state", attrs["quest_state"]));
                        }
                    }

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

    public static async Task ClaimQuestReward(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var commonCore = McpCommon.Profile("common_core");
        var theater = McpCommon.Profile("theater0");
        var rewardsRoot = (DataStore.Response("Campaign/rewards.json") as JsonObject)?["quest"] as JsonObject
                          ?? new JsonObject();

        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var theaterChanged = false;
        var commonCoreChanged = false;

        var queryProfileId = RequestUtil.Query(ctx, "profileId");
        var questId = body.GetStr("questId");

        if (!string.IsNullOrEmpty(questId))
        {
            var items = McpCommon.Items(profile);

            string questTemplateId = null;
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                if (questId.Equals(key, StringComparison.OrdinalIgnoreCase))
                    questTemplateId = (items[key] as JsonObject)?.GetStr("templateId")?.ToLowerInvariant();
            }

            if (!string.IsNullOrEmpty(questTemplateId) && rewardsRoot[questTemplateId] is JsonObject rewardEntry)
            {
                JsonArray rewards;
                var selectedIndex = body.GetLong("selectedRewardIndex");

                if (selectedIndex != -1 && rewardEntry["selectableRewards"] is JsonArray selectable &&
                    selectedIndex >= 0 && selectedIndex < selectable.Count)
                {
                    rewards = (selectable[(int)selectedIndex] as JsonObject)?["rewards"] as JsonArray;
                }
                else
                {
                    rewards = rewardEntry["rewards"] as JsonArray;
                }

                var theaterUpdate = new JsonObject
                {
                    ["profileRevision"] = theater.GetLong("rvn"),
                    ["profileId"] = "theater0",
                    ["profileChangesBaseRevision"] = theater.GetLong("rvn"),
                    ["profileChanges"] = new JsonArray(),
                    ["profileCommandRevision"] = theater.GetLong("commandRevision")
                };
                multiUpdate.Add(theaterUpdate);

                JsonObject commonCoreUpdate = null;
                if (queryProfileId == "campaign")
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
                }

                var notification = new JsonObject
                {
                    ["type"] = "questClaim",
                    ["primary"] = true,
                    ["questId"] = questTemplateId,
                    ["loot"] = new JsonObject { ["items"] = new JsonArray() }
                };
                notifications.Add(notification);
                var lootItems = (JsonArray)((JsonObject)notification["loot"]!)["items"]!;
                var theaterChanges = (JsonArray)theaterUpdate["profileChanges"]!;

                if (rewards != null)
                {
                    foreach (var rewardNode in rewards)
                    {
                        if (rewardNode is not JsonObject reward) continue;

                        var id = Functions.MakeId();
                        var templateId = (reward.GetStr("templateId") ?? "").ToLowerInvariant();
                        var quantity = reward.GetLong("quantity");

                        if (templateId.StartsWith("weapon:") || templateId.StartsWith("trap:") ||
                            templateId.StartsWith("ammo:"))
                        {
                            var item = McpCommon.NewItem(reward.GetStr("templateId") ?? "", new JsonObject
                            {
                                ["clipSizeScale"] = 0,
                                ["loadedAmmo"] = 999,
                                ["level"] = 1,
                                ["alterationDefinitions"] = new JsonArray(),
                                ["baseClipSize"] = 999,
                                ["durability"] = 375,
                                ["itemSource"] = "",
                                ["item_seen"] = false
                            }, quantity);

                            McpCommon.Items(theater)[id] = item;
                            theaterChanges.Add(McpCommon.ItemAdded(id, item));

                            lootItems.Add(new JsonObject
                            {
                                ["itemType"] = reward.GetStr("templateId"),
                                ["itemGuid"] = id,
                                ["itemProfile"] = "theater0",
                                ["quantity"] = quantity
                            });

                            theaterChanged = true;
                        }
                        else if (queryProfileId == "campaign" &&
                                 (templateId.StartsWith("homebasebannericon:") || templateId == "token:founderchatunlock"))
                        {
                            var item = McpCommon.NewItem(reward.GetStr("templateId") ?? "", new JsonObject
                            {
                                ["max_level_bonus"] = 0,
                                ["level"] = 1,
                                ["item_seen"] = false,
                                ["xp"] = 0,
                                ["favorite"] = false
                            }, quantity);

                            McpCommon.Items(commonCore)[id] = item;
                            ((JsonArray)commonCoreUpdate!["profileChanges"]!).Add(McpCommon.ItemAdded(id, item));

                            lootItems.Add(new JsonObject
                            {
                                ["itemType"] = reward.GetStr("templateId"),
                                ["itemGuid"] = id,
                                ["itemProfile"] = "common_core",
                                ["quantity"] = quantity
                            });

                            commonCoreChanged = true;
                        }
                        else
                        {
                            var attributes = new JsonObject
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

                            if (templateId.StartsWith("quest:"))
                                attributes["quest_state"] = "Active";

                            var item = McpCommon.NewItem(reward.GetStr("templateId") ?? "", attributes, quantity);
                            McpCommon.Items(profile)[id] = item;
                            changes.Add(McpCommon.ItemAdded(id, item));

                            lootItems.Add(new JsonObject
                            {
                                ["itemType"] = reward.GetStr("templateId"),
                                ["itemGuid"] = id,
                                ["itemProfile"] = queryProfileId,
                                ["quantity"] = quantity
                            });
                        }
                    }
                }
            }

            var quest = McpCommon.Item(profile, questId);
            if (quest?["attributes"] is JsonObject questAttrs)
            {
                questAttrs["quest_state"] = "Claimed";
                questAttrs["last_state_change_time"] = Functions.IsoNow();
            }
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            if (theaterChanged)
            {
                McpCommon.Increment(theater);
                var theaterUpdate = multiUpdate[0] as JsonObject;
                theaterUpdate!["profileRevision"] = theater.GetLong("rvn");
                theaterUpdate["profileCommandRevision"] = theater.GetLong("commandRevision");
                McpCommon.Save("theater0", theater);
            }

            if (commonCoreChanged)
            {
                McpCommon.Increment(commonCore);
                var commonCoreUpdate = multiUpdate[1] as JsonObject;
                commonCoreUpdate!["profileRevision"] = commonCore.GetLong("rvn");
                commonCoreUpdate["profileCommandRevision"] = commonCore.GetLong("commandRevision");
                McpCommon.Save("common_core", commonCore);
            }

            var claimed = McpCommon.Item(profile, questId);
            if (claimed?["attributes"] is JsonObject claimedAttrs)
            {
                changes.Add(McpCommon.ItemAttrChanged(questId, "quest_state", claimedAttrs["quest_state"]));
                changes.Add(McpCommon.ItemAttrChanged(questId, "last_state_change_time",
                    claimedAttrs["last_state_change_time"]));
            }

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            multiUpdate: multiUpdate, notifications: notifications);
    }
}
