using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpLoadoutHandlers
{
    public static async Task AssignTeamPerkToLoadout(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profile = McpCommon.Profile("campaign");
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var loadoutId = body.GetStr("loadoutId");
        if (!string.IsNullOrEmpty(loadoutId))
        {
            var attrs = ItemAttrs(profile, loadoutId);
            if (attrs != null)
            {
                attrs["team_perk"] = body.GetStr("teamPerkId") ?? "";
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "team_perk", ItemAttrs(profile, loadoutId)?["team_perk"]));
            McpCommon.Save("campaign", profile);
        }

        await McpCommon.Finish(ctx, "campaign", profile, baseRevision, changes);
    }

    public static async Task AssignGadgetToLoadout(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profile = McpCommon.Profile("campaign");
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var loadoutId = body.GetStr("loadoutId");
        var gadgetId = body.GetStr("gadgetId") ?? "";
        var slotIndex = body.GetLong("slotIndex");

        if (!string.IsNullOrEmpty(loadoutId))
        {
            var attrs = ItemAttrs(profile, loadoutId);
            var gadgets = attrs?.GetArr("gadgets");
            if (gadgets != null && gadgets.Count >= 2)
            {
                var gadget0 = gadgets[0] as JsonObject;
                var gadget1 = gadgets[1] as JsonObject;

                switch (slotIndex)
                {
                    case 0:
                        if (gadgetId.ToLowerInvariant() == (gadget1?.GetStr("gadget") ?? "").ToLowerInvariant())
                            gadget1!["gadget"] = "";
                        gadget0!["gadget"] = gadgetId;
                        changed = true;
                        break;
                    case 1:
                        if (gadgetId.ToLowerInvariant() == (gadget0?.GetStr("gadget") ?? "").ToLowerInvariant())
                            gadget0!["gadget"] = "";
                        gadget1!["gadget"] = gadgetId;
                        changed = true;
                        break;
                }
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "gadgets", ItemAttrs(profile, loadoutId)?["gadgets"]));
            McpCommon.Save("campaign", profile);
        }

        await McpCommon.Finish(ctx, "campaign", profile, baseRevision, changes);
    }

    public static async Task AssignWorkerToSquad(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var characterId = body.GetStr("characterId");
        var squadId = body.GetStr("squadId") ?? "";
        var slotIndex = body.GetLong("slotIndex");

        if (!string.IsNullOrEmpty(characterId))
        {
            var items = McpCommon.Items(profile);
            foreach (var key in items.Select(kv => kv.Key).ToList())
            {
                var attrs = (items[key] as JsonObject)?["attributes"] as JsonObject;
                if (attrs == null || !attrs.ContainsKey("squad_id") || !attrs.ContainsKey("squad_slot_idx")) continue;

                if (attrs.GetStr("squad_id") != "" && attrs.GetLong("squad_slot_idx") != -1 &&
                    (attrs.GetStr("squad_id") ?? "").ToLowerInvariant() == squadId.ToLowerInvariant() &&
                    attrs.GetLong("squad_slot_idx") == slotIndex)
                {
                    attrs["squad_id"] = "";
                    attrs["squad_slot_idx"] = 0;

                    changes.Add(McpCommon.ItemAttrChanged(key, "squad_id", attrs["squad_id"]));
                    changes.Add(McpCommon.ItemAttrChanged(key, "squad_slot_idx", attrs["squad_slot_idx"]));
                }
            }

            var target = ItemAttrs(profile, characterId);
            if (target != null)
            {
                target["squad_id"] = squadId;
                target["squad_slot_idx"] = slotIndex;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var target = ItemAttrs(profile, characterId);
            changes.Add(McpCommon.ItemAttrChanged(characterId, "squad_id", target?["squad_id"]));
            changes.Add(McpCommon.ItemAttrChanged(characterId, "squad_slot_idx", target?["squad_slot_idx"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task AssignWorkerToSquadBatch(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "profile0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var characterIds = body["characterIds"] as JsonArray;
        var squadIds = body["squadIds"] as JsonArray;
        var slotIndices = body["slotIndices"] as JsonArray;

        if (characterIds != null && squadIds != null && slotIndices != null)
        {
            for (var i = 0; i < characterIds.Count; i++)
            {
                var characterId = J.AsString(characterIds[i]);
                var squadId = i < squadIds.Count ? J.AsString(squadIds[i]) ?? "" : "";
                var slotIndex = i < slotIndices.Count ? J.AsLong(slotIndices[i]) : 0;

                if (string.IsNullOrEmpty(characterId)) continue;

                var items = McpCommon.Items(profile);
                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var attrs = (items[key] as JsonObject)?["attributes"] as JsonObject;
                    if (attrs == null || !attrs.ContainsKey("squad_id") || !attrs.ContainsKey("squad_slot_idx")) continue;

                    if (attrs.GetStr("squad_id") != "" && attrs.GetLong("squad_slot_idx") != -1 &&
                        (attrs.GetStr("squad_id") ?? "").ToLowerInvariant() == squadId.ToLowerInvariant() &&
                        attrs.GetLong("squad_slot_idx") == slotIndex)
                    {
                        attrs["squad_id"] = "";
                        attrs["squad_slot_idx"] = 0;

                        changes.Add(McpCommon.ItemAttrChanged(key, "squad_id", attrs["squad_id"]));
                        changes.Add(McpCommon.ItemAttrChanged(key, "squad_slot_idx", attrs["squad_slot_idx"]));
                    }
                }

                var target = ItemAttrs(profile, characterId);
                if (target != null)
                {
                    target["squad_id"] = squadId;
                    target["squad_slot_idx"] = slotIndex;
                }

                changes.Add(McpCommon.ItemAttrChanged(characterId, "squad_id", target?["squad_id"]));
                changes.Add(McpCommon.ItemAttrChanged(characterId, "squad_slot_idx", target?["squad_slot_idx"]));
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

    public static async Task ModifyQuickbar(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "theater0");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var attrs = McpCommon.Attrs(profile);
        var playerLoadout = attrs["player_loadout"] as JsonObject;

        if (body["primaryQuickbarChoices"] is JsonObject primary)
        {
            var slots = playerLoadout?["primaryQuickBarRecord"]?["slots"] as JsonObject;
            if (slots != null)
            {
                foreach (var pair in primary)
                {
                    if (!long.TryParse(pair.Key, out var index)) continue;
                    var slot = slots[(index + 1).ToString()] as JsonObject;
                    if (slot == null) continue;

                    var choice = J.AsString(pair.Value) ?? "";
                    slot["items"] = choice == ""
                        ? new JsonArray()
                        : new JsonArray { choice.Replace("-", "").ToUpperInvariant() };
                }
            }
            changed = true;
        }

        var secondary = body.GetStr("secondaryQuickbarChoice");
        if (secondary != null)
        {
            var slot = playerLoadout?["secondaryQuickBarRecord"]?["slots"]?["5"] as JsonObject;
            if (slot != null)
            {
                slot["items"] = secondary == ""
                    ? new JsonArray()
                    : new JsonArray { secondary.Replace("-", "").ToUpperInvariant() };
            }
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("player_loadout", attrs["player_loadout"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task AssignHeroToLoadout(HttpContext ctx)
    {
        await AssignCrewMember(ctx, "campaign", "heroId", new[]
        {
            "commanderslot", "followerslot1", "followerslot2", "followerslot3", "followerslot4", "followerslot5"
        });
    }

    public static async Task AssignDefenderToLoadout(HttpContext ctx)
    {
        await AssignCrewMember(ctx, "campaign", "defenderId", new[]
        {
            "defenderslot1", "defenderslot2", "defenderslot3"
        });
    }

    private static async Task AssignCrewMember(HttpContext ctx, string defaultProfile, string idField, string[] validSlots)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, defaultProfile);
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var loadoutId = body.GetStr("loadoutId");
        var slotName = body.GetStr("slotName");
        var memberId = body.GetStr(idField);

        if (!string.IsNullOrEmpty(loadoutId) && !string.IsNullOrEmpty(slotName) && memberId != null)
        {
            var targetSlot = slotName.ToLowerInvariant();
            var memberLower = memberId.ToLowerInvariant();

            if (validSlots.Contains(targetSlot))
            {
                var attrs = ItemAttrs(profile, loadoutId);
                if (attrs?["crew_members"] is JsonObject crew)
                {
                    foreach (var slot in validSlots)
                    {
                        if ((crew.GetStr(slot) ?? "").ToLowerInvariant() == memberLower)
                            crew[slot] = "";
                    }

                    crew[targetSlot] = memberId;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "crew_members", ItemAttrs(profile, loadoutId)?["crew_members"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task AssignWeaponToDefender(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var defenderId = body.GetStr("defenderId");
        if (!string.IsNullOrEmpty(defenderId))
        {
            var attrs = ItemAttrs(profile, defenderId);
            if (attrs != null)
            {
                attrs["weapon_schematic"] = body.GetStr("weaponSchematicId") ?? "";
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.ItemAttrChanged(defenderId, "weapon_schematic", ItemAttrs(profile, defenderId)?["weapon_schematic"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task ClearHeroLoadout(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var loadoutId = body.GetStr("loadoutId");
        if (!string.IsNullOrEmpty(loadoutId))
        {
            var item = McpCommon.Item(profile, loadoutId);
            if (item != null)
            {
                var oldAttrs = item["attributes"] as JsonObject ?? new JsonObject();
                var oldCrew = oldAttrs["crew_members"] as JsonObject;

                item["attributes"] = new JsonObject
                {
                    ["team_perk"] = "",
                    ["loadout_name"] = oldAttrs["loadout_name"]?.DeepClone(),
                    ["crew_members"] = new JsonObject
                    {
                        ["followerslot5"] = "",
                        ["followerslot4"] = "",
                        ["followerslot3"] = "",
                        ["followerslot2"] = "",
                        ["followerslot1"] = "",
                        ["commanderslot"] = oldCrew?["commanderslot"]?.DeepClone()
                    },
                    ["loadout_index"] = oldAttrs["loadout_index"]?.DeepClone(),
                    ["gadgets"] = new JsonArray
                    {
                        new JsonObject { ["gadget"] = "", ["slot_index"] = 0 },
                        new JsonObject { ["gadget"] = "", ["slot_index"] = 1 }
                    }
                };

                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = ItemAttrs(profile, loadoutId);
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "team_perk", attrs?["team_perk"]));
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "crew_members", attrs?["crew_members"]));
            changes.Add(McpCommon.ItemAttrChanged(loadoutId, "gadgets", attrs?["gadgets"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetActiveHeroLoadout(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body.ContainsKey("selectedLoadout"))
        {
            var attrs = McpCommon.Attrs(profile);
            attrs["selected_hero_loadout"] = body["selectedLoadout"]?.DeepClone();
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("selected_hero_loadout", McpCommon.Attrs(profile)["selected_hero_loadout"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task UnassignAllSquads(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body["squadIds"] is JsonArray squadIds)
        {
            foreach (var idNode in squadIds)
            {
                var id = J.AsString(idNode) ?? "";
                var items = McpCommon.Items(profile);
                foreach (var key in items.Select(kv => kv.Key).ToList())
                {
                    var attrs = (items[key] as JsonObject)?["attributes"] as JsonObject;
                    if (attrs == null || !attrs.ContainsKey("squad_id")) continue;
                    if ((attrs.GetStr("squad_id") ?? "").ToLowerInvariant() != id.ToLowerInvariant()) continue;

                    attrs["squad_id"] = "";
                    changes.Add(McpCommon.ItemAttrChanged(key, "squad_id", attrs["squad_id"]));
                }            }
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Save(profileId, profile);
        }

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
