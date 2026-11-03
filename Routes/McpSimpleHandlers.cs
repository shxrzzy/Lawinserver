using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpSimpleHandlers
{
    public static async Task SetAffiliateName(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profile = McpCommon.Profile("common_core");
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var affiliateName = body.GetStr("affiliateName") ?? "";
        var supported = DataStore.Response("SAC.json") as JsonArray;
        if (supported != null)
        {
            foreach (var codeNode in supported)
            {
                var code = J.AsString(codeNode) ?? "";
                if (affiliateName.Equals(code, StringComparison.OrdinalIgnoreCase) || affiliateName == "")
                {
                    var attrs = McpCommon.Attrs(profile);
                    attrs["mtx_affiliate_set_time"] = Functions.IsoNow();
                    attrs["mtx_affiliate"] = affiliateName;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Attrs(profile);
            changes.Add(McpCommon.StatModified("mtx_affiliate_set_time", attrs["mtx_affiliate_set_time"]));
            changes.Add(McpCommon.StatModified("mtx_affiliate", attrs["mtx_affiliate"]));
            McpCommon.Save("common_core", profile);
        }

        await McpCommon.Finish(ctx, "common_core", profile, baseRevision, changes);
    }

    public static async Task SetHomebaseBanner(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var icon = body.GetStr("homebaseBannerIconId");
        var color = body.GetStr("homebaseBannerColorId");

        if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(color))
        {
            var attrs = McpCommon.Attrs(profile);
            switch (profileId)
            {
                case "profile0":
                    if (attrs["homebase"] is JsonObject homebase)
                    {
                        homebase["bannerIconId"] = icon;
                        homebase["bannerColorId"] = color;
                    }
                    changed = true;
                    break;
                case "common_public":
                    attrs["banner_icon"] = icon;
                    attrs["banner_color"] = color;
                    changed = true;
                    break;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Attrs(profile);
            if (profileId == "profile0")
                changes.Add(McpCommon.StatModified("homebase", attrs["homebase"]));
            if (profileId == "common_public")
            {
                changes.Add(McpCommon.StatModified("banner_icon", attrs["banner_icon"]));
                changes.Add(McpCommon.StatModified("banner_color", attrs["banner_color"]));
            }
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetHomebaseName(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var name = body.GetStr("homebaseName");
        if (!string.IsNullOrEmpty(name))
        {
            var attrs = McpCommon.Attrs(profile);
            switch (profileId)
            {
                case "profile0":
                    if (attrs["homebase"] is JsonObject homebase) homebase["townName"] = name;
                    changed = true;
                    break;
                case "common_public":
                    attrs["homebase_name"] = name;
                    changed = true;
                    break;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Attrs(profile);
            if (profileId == "profile0")
                changes.Add(McpCommon.StatModified("homebase", attrs["homebase"]));
            if (profileId == "common_public")
                changes.Add(McpCommon.StatModified("homebase_name", attrs["homebase_name"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task PurchaseHomebaseNode(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var id = Functions.MakeId();

        var nodeId = body.GetStr("nodeId");
        if (!string.IsNullOrEmpty(nodeId))
        {
            McpCommon.Items(profile)[id] = McpCommon.NewItem($"HomebaseNode:{nodeId}", new JsonObject
            {
                ["item_seen"] = true
            });
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAdded(id, McpCommon.Item(profile, id)));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task RemoveGiftBox(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var single = body.GetStr("giftBoxItemId");
        if (!string.IsNullOrEmpty(single))
        {
            McpCommon.Items(profile).Remove(single);
            changes.Add(McpCommon.ItemRemoved(single));
            changed = true;
        }

        if (body["giftBoxItemIds"] is JsonArray ids)
        {
            foreach (var idNode in ids)
            {
                var id = J.AsString(idNode);
                if (string.IsNullOrEmpty(id)) continue;
                McpCommon.Items(profile).Remove(id);
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

    public static async Task SetPartyAssistQuest(HttpContext ctx)
    {
        await SetStringStat(ctx, "athena", "party_assist_quest", McpCommon.Body(ctx).GetStr("questToPinAsPartyAssist") ?? "");
    }

    public static async Task AthenaPinQuest(HttpContext ctx)
    {
        await SetStringStat(ctx, "athena", "pinned_quest", McpCommon.Body(ctx).GetStr("pinnedQuest") ?? "");
    }

    private static async Task SetStringStat(HttpContext ctx, string defaultProfile, string statName, string value)
    {
        var profileId = McpCommon.ProfileId(ctx, defaultProfile);
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var attrs = McpCommon.Attrs(profile);
        var changed = false;

        if (attrs.ContainsKey(statName))
        {
            attrs[statName] = value;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified(statName, attrs[statName]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetPinnedQuests(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var attrs = McpCommon.Attrs(profile);
        var changed = false;

        if (body["pinnedQuestIds"] is JsonArray pinned)
        {
            if (attrs["client_settings"] is not JsonObject clientSettings)
            {
                clientSettings = new JsonObject();
                attrs["client_settings"] = clientSettings;
            }
            clientSettings["pinnedQuestInstances"] = pinned.DeepClone();
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("client_settings", attrs["client_settings"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task MarkNewQuestNotificationSent(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["itemIds"] is JsonArray ids)
        {
            foreach (var idNode in ids)
            {
                var id = J.AsString(idNode);
                if (string.IsNullOrEmpty(id)) continue;
                var item = McpCommon.Item(profile, id);
                if (item?["attributes"] is not JsonObject attrs) continue;
                attrs["sent_new_notification"] = true;
                changes.Add(McpCommon.ItemAttrChanged(id, "sent_new_notification", true));
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

    public static async Task IncrementNamedCounterStat(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var attrs = McpCommon.Attrs(profile);
        var changed = false;

        var counterName = body.GetStr("counterName");
        if (!string.IsNullOrEmpty(counterName) && attrs["named_counters"] is JsonObject counters &&
            counters[counterName] is JsonObject counter)
        {
            counter["current_count"] = counter.GetLong("current_count") + 1;
            counter["last_incremented_time"] = Functions.IsoNow();
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("named_counters", attrs["named_counters"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task ClaimLoginReward(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var memory = Functions.GetVersionInfo(ctx);
        var attrs = McpCommon.Attrs(profile);

        var dateFormat = Functions.IsoNow().Split('T')[0] + "T00:00:00.000Z";

        if (attrs["daily_rewards"] is JsonObject dailyRewards && dailyRewards.GetStr("lastClaimDate") != dateFormat)
        {
            dailyRewards["nextDefaultReward"] = dailyRewards.GetLong("nextDefaultReward") + 1;
            dailyRewards["totalDaysLoggedIn"] = dailyRewards.GetLong("totalDaysLoggedIn") + 1;
            dailyRewards["lastClaimDate"] = dateFormat;

            if (dailyRewards["additionalSchedules"]?["founderspackdailyrewardtoken"] is JsonObject founders)
                founders["rewardsClaimed"] = founders.GetLong("rewardsClaimed") + 1;

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("daily_rewards", attrs["daily_rewards"]));

            if (memory.Season < 7 && attrs["daily_rewards"] is JsonObject daily)
            {
                var day = daily.GetLong("totalDaysLoggedIn") % 336;
                var dailyRewardsData = DataStore.Response("Campaign/dailyRewards.json") as JsonArray;
                notifications.Add(new JsonObject
                {
                    ["type"] = "daily_rewards",
                    ["primary"] = true,
                    ["daysLoggedIn"] = daily.GetLong("totalDaysLoggedIn"),
                    ["items"] = new JsonArray { dailyRewardsData?[(int)day]?.DeepClone() }
                });
            }

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, notifications: notifications);
    }

    public static async Task SetSeasonPassAutoClaim(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var attrs = McpCommon.Attrs(profile);
        var changed = false;

        if (body["seasonIds"] is JsonArray seasonIds && body.ContainsKey("bEnabled"))
        {
            if (attrs["auto_spend_season_currency_ids"] is not JsonArray currencyIds)
            {
                currencyIds = new JsonArray();
                attrs["auto_spend_season_currency_ids"] = currencyIds;
            }

            var enabled = body.GetBool("bEnabled");
            foreach (var seasonIdNode in seasonIds)
            {
                var seasonId = J.AsString(seasonIdNode);
                if (seasonId == null) continue;

                if (enabled)
                {
                    if (!currencyIds.Any(n => J.AsString(n) == seasonId))
                    {
                        currencyIds.Add(seasonId);
                        changed = true;
                    }
                }
                else
                {
                    for (var i = currencyIds.Count - 1; i >= 0; i--)
                    {
                        if (J.AsString(currencyIds[i]) == seasonId)
                        {
                            currencyIds.RemoveAt(i);
                            changed = true;
                        }
                    }
                }
            }
        }

        if (changed)
        {
            changes.Add(McpCommon.StatModified("auto_spend_season_currency_ids", attrs["auto_spend_season_currency_ids"]));
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetItemArchivedStatusBatch(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["itemIds"] is JsonArray ids)
        {
            var archived = body.GetBool("archived");
            foreach (var idNode in ids)
            {
                var id = J.AsString(idNode);
                if (id == null) continue;
                var item = McpCommon.Item(profile, id);
                if (item?["attributes"] is not JsonObject attrs) continue;
                attrs["archived"] = archived;
                changes.Add(McpCommon.ItemAttrChanged(id, "archived", archived));
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

    public static async Task SetItemFavoriteStatusBatch(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["itemIds"] is JsonArray ids)
        {
            var statuses = body["itemFavStatus"] as JsonArray;
            for (var i = 0; i < ids.Count; i++)
            {
                var id = J.AsString(ids[i]);
                if (id == null) continue;
                var item = McpCommon.Item(profile, id);
                if (item?["attributes"] is not JsonObject attrs) continue;

                var favorite = statuses != null && i < statuses.Count ? J.AsBool(statuses[i]) : false;
                attrs["favorite"] = favorite;
                changes.Add(McpCommon.ItemAttrChanged(id, "favorite", favorite));
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

    public static async Task SetItemFavoriteStatus(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var target = body.GetStr("targetItemId");
        if (!string.IsNullOrEmpty(target))
        {
            var item = McpCommon.Item(profile, target);
            if (item?["attributes"] is JsonObject attrs)
            {
                var favorite = body.GetBool("bFavorite");
                attrs["favorite"] = favorite;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(target, "favorite", McpCommon.Item(profile, target)?["attributes"]?["favorite"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task MarkItemSeen(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["itemIds"] is JsonArray ids)
        {
            foreach (var idNode in ids)
            {
                var id = J.AsString(idNode);
                if (string.IsNullOrEmpty(id)) continue;
                var item = McpCommon.Item(profile, id);
                if (item?["attributes"] is not JsonObject attrs) continue;
                attrs["item_seen"] = true;
                changes.Add(McpCommon.ItemAttrChanged(id, "item_seen", true));
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

    public static async Task SetActiveArchetype(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var attrs = McpCommon.Attrs(profile);
        var changed = false;

        var group = body.GetStr("archetypeGroup");
        if (!string.IsNullOrEmpty(group))
        {
            if (attrs["loadout_archetype_values"] is not JsonObject values)
            {
                values = new JsonObject();
                attrs["loadout_archetype_values"] = values;
            }
            values[group] = body.GetStr("archetype") ?? "";
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("loadout_archetype_values", attrs["loadout_archetype_values"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetHeroCosmeticVariants(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var heroItem = body.GetStr("heroItem");
        if (body["outfitVariants"] != null && body["backblingVariants"] != null && !string.IsNullOrEmpty(heroItem))
        {
            var item = McpCommon.Item(profile, heroItem);
            if (item?["attributes"] is JsonObject attrs)
            {
                attrs["outfitvariants"] = body["outfitVariants"]?.DeepClone();
                attrs["backblingvariants"] = body["backblingVariants"]?.DeepClone();
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Item(profile, heroItem)?["attributes"];
            changes.Add(McpCommon.ItemAttrChanged(heroItem, "outfitvariants", attrs?["outfitvariants"]));
            changes.Add(McpCommon.ItemAttrChanged(heroItem, "backblingvariants", attrs?["backblingvariants"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task GenericClient(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        await McpCommon.Finish(ctx, profileId, profile, profile.GetLong("rvn"), new JsonArray());
    }

    public static async Task GenericDedicated(HttpContext ctx)
    {
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        await McpCommon.Finish(ctx, profileId, profile, profile.GetLong("rvn"), new JsonArray());
    }
}
