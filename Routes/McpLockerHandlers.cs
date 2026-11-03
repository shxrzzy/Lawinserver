using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpLockerHandlers
{
    public static async Task EquipBattleRoyaleCustomization(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profile = McpCommon.Profile("athena");
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var statChanged = false;
        var variantChanged = false;

        var attrs = McpCommon.Attrs(profile);
        try
        {
            if (!attrs.ContainsKey("favorite_dance"))
                attrs["favorite_dance"] = new JsonArray("", "", "", "", "", "");
            if (!attrs.ContainsKey("favorite_itemwraps"))
                attrs["favorite_itemwraps"] = new JsonArray("", "", "", "", "", "", "");
        }
        catch
        {
            
        }

        var itemToSlot = body.GetStr("itemToSlot");

        try
        {
            var variantUpdates = body["variantUpdates"] as JsonArray ?? new JsonArray();
            var asString = variantUpdates.ToJsonString();

            if (asString.Contains("active"))
            {
                var itemAttrs = McpCommon.Item(profile, itemToSlot ?? "")?["attributes"] as JsonObject;
                if (itemAttrs != null)
                {
                    if (itemAttrs["variants"] is not JsonArray variants || variants.Count == 0)
                    {
                        variants = variantUpdates.DeepClone() as JsonArray ?? new JsonArray();
                        itemAttrs["variants"] = variants;
                    }

                    for (var i = 0; i < variants.Count; i++)
                    {
                        try
                        {
                            if (i >= variantUpdates.Count) continue;

                            var variant = variants[i] as JsonObject;
                            var update = variantUpdates[i] as JsonObject;
                            if (variant == null || update == null) continue;

                            if ((variant.GetStr("channel") ?? "").ToLowerInvariant() ==
                                (update.GetStr("channel") ?? "").ToLowerInvariant())
                            {
                                variant["active"] = update.GetStr("active") ?? "";
                            }
                        }
                        catch
                        {
                            
                        }
                    }

                    variantChanged = true;
                }
            }
        }
        catch
        {
            
        }

        var slotName = body.GetStr("slotName");
        if (slotName != null)
        {
            switch (slotName)
            {
                case "Character":
                case "Backpack":
                case "Pickaxe":
                case "Glider":
                case "SkyDiveContrail":
                case "MusicPack":
                case "LoadingScreen":
                    attrs[$"favorite_{slotName.ToLowerInvariant()}"] = itemToSlot ?? "";
                    statChanged = true;
                    break;

                case "Dance":
                    var danceIndex = body.GetLong("indexWithinSlot");
                    if (Math.Sign(danceIndex) == 1 || Math.Sign(danceIndex) == 0)
                    {
                        var dances = attrs["favorite_dance"] as JsonArray;
                        if (dances != null && danceIndex >= 0 && danceIndex < dances.Count)
                            dances[(int)danceIndex] = itemToSlot ?? "";
                    }
                    statChanged = true;
                    break;

                case "ItemWrap":
                    var wrapIndex = body.GetLong("indexWithinSlot");
                    var wraps = attrs["favorite_itemwraps"] as JsonArray;
                    switch (Math.Sign(wrapIndex))
                    {
                        case 0:
                        case 1:
                            if (wraps != null && wrapIndex >= 0 && wrapIndex < wraps.Count)
                                wraps[(int)wrapIndex] = itemToSlot ?? "";
                            break;

                        case -1:
                            if (wraps != null)
                            {
                                for (var i = 0; i < 7 && i < wraps.Count; i++)
                                    wraps[i] = itemToSlot ?? "";
                            }
                            break;
                    }
                    statChanged = true;
                    break;
            }
        }

        if (statChanged)
        {
            var category = $"favorite_{slotName ?? "character"}".ToLowerInvariant();
            if (category == "favorite_itemwrap") category += "s";

            McpCommon.Increment(profile);

            changes.Add(McpCommon.StatModified(category, attrs[category]));

            if (variantChanged)
            {
                changes.Add(McpCommon.ItemAttrChanged(itemToSlot ?? "", "variants",
                    (McpCommon.Item(profile, itemToSlot ?? "")?["attributes"] as JsonObject)?["variants"]));
            }

            McpCommon.Save("athena", profile);
        }

        await McpCommon.Finish(ctx, "athena", profile, baseRevision, changes);
    }

    public static async Task SetBattleRoyaleBanner(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profile = McpCommon.Profile("athena");
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var iconId = body.GetStr("homebaseBannerIconId");
        var colorId = body.GetStr("homebaseBannerColorId");

        if (!string.IsNullOrEmpty(iconId) && !string.IsNullOrEmpty(colorId))
        {
            var attrs = McpCommon.Attrs(profile);
            attrs["banner_icon"] = iconId;
            attrs["banner_color"] = colorId;
            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Attrs(profile);
            changes.Add(McpCommon.StatModified("banner_icon", attrs["banner_icon"]));
            changes.Add(McpCommon.StatModified("banner_color", attrs["banner_color"]));
            McpCommon.Save("athena", profile);
        }

        await McpCommon.Finish(ctx, "athena", profile, baseRevision, changes);
    }

    public static async Task SetCosmeticLockerBanner(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var icon = body.GetStr("bannerIconTemplateName");
        var color = body.GetStr("bannerColorTemplateName");
        var lockerItem = body.GetStr("lockerItem");

        if (!string.IsNullOrEmpty(icon) && !string.IsNullOrEmpty(color) && !string.IsNullOrEmpty(lockerItem))
        {
            var attrs = McpCommon.Item(profile, lockerItem)?["attributes"] as JsonObject;
            if (attrs != null)
            {
                attrs["banner_icon_template"] = icon;
                attrs["banner_color_template"] = color;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var attrs = McpCommon.Item(profile, lockerItem!)?["attributes"] as JsonObject;
            changes.Add(McpCommon.ItemAttrChanged(lockerItem!, "banner_icon_template", attrs?["banner_icon_template"]));
            changes.Add(McpCommon.ItemAttrChanged(lockerItem!, "banner_color_template", attrs?["banner_color_template"]));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task SetCosmeticLockerSlot(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var itemToSlot = body.GetStr("itemToSlot");
        var lockerItem = body.GetStr("lockerItem");
        var category = body.GetStr("category");

        try
        {
            var variantUpdates = body["variantUpdates"] as JsonArray ?? new JsonArray();
            var asString = variantUpdates.ToJsonString();

            if (asString.Contains("active"))
            {
                var newVariants = new JsonArray
                {
                    new JsonObject { ["variants"] = new JsonArray() }
                };

                if (profile.GetStr("profileId") == "athena")
                {
                    var itemAttrs = McpCommon.Item(profile, itemToSlot ?? "")?["attributes"] as JsonObject;
                    if (itemAttrs?["variants"] is JsonArray variants)
                    {
                        foreach (var updateNode in variantUpdates)
                        {
                            if (updateNode is not JsonObject update) continue;

                            var found = false;
                            foreach (var variantNode in variants)
                            {
                                if (variantNode is not JsonObject variant) continue;
                                if (variant.GetStr("channel") != update.GetStr("channel")) continue;

                                variant["active"] = update["active"]?.DeepClone();
                                found = true;
                                break;
                            }

                            if (!found)
                                variants.Add(update.DeepClone());
                        }

                        changes.Add(McpCommon.ItemAttrChanged(itemToSlot ?? "", "variants", variants));
                        changed = true;
                    }
                }

                var newVariantList = (JsonArray)((JsonObject)newVariants[0]!)["variants"]!;
                foreach (var updateNode in variantUpdates)
                {
                    if (updateNode is not JsonObject update) continue;

                    newVariantList.Add(new JsonObject
                    {
                        ["channel"] = update["channel"]?.DeepClone(),
                        ["active"] = update["active"]?.DeepClone()
                    });

                    var lockerSlots = (McpCommon.Item(profile, lockerItem ?? "")?["attributes"] as JsonObject)
                        ?.GetObj("locker_slots_data");
                    var slot = lockerSlots?.GetObj("slots")?.GetObj(category ?? "");
                    if (slot != null)
                        slot["activeVariants"] = newVariants.DeepClone();
                }
            }
        }
        catch
        {
            
        }

        if (category != null && lockerItem != null)
        {
            var lockerSlots = (McpCommon.Item(profile, lockerItem)?["attributes"] as JsonObject)
                ?.GetObj("locker_slots_data");
            var slots = lockerSlots?.GetObj("slots");

            switch (category)
            {
                case "Character":
                case "Backpack":
                case "Pickaxe":
                case "Glider":
                case "SkyDiveContrail":
                case "MusicPack":
                case "LoadingScreen":
                    if (slots?[category] is JsonObject slot)
                        slot["items"] = new JsonArray(itemToSlot ?? "");
                    changed = true;
                    break;

                case "Dance":
                    var danceIndex = body.GetLong("slotIndex");
                    if (Math.Sign(danceIndex) == 1 || Math.Sign(danceIndex) == 0)
                    {
                        if (slots?.GetObj("Dance")?.GetArr("items") is JsonArray danceItems &&
                            danceIndex >= 0 && danceIndex < danceItems.Count)
                        {
                            danceItems[(int)danceIndex] = itemToSlot ?? "";
                        }
                    }
                    changed = true;
                    break;

                case "ItemWrap":
                    var wrapIndex = body.GetLong("slotIndex");
                    var wrapItems = slots?.GetObj("ItemWrap")?.GetArr("items");
                    switch (Math.Sign(wrapIndex))
                    {
                        case 0:
                        case 1:
                            if (wrapItems != null && wrapIndex >= 0 && wrapIndex < wrapItems.Count)
                                wrapItems[(int)wrapIndex] = itemToSlot ?? "";
                            break;

                        case -1:
                            if (wrapItems != null)
                            {
                                for (var i = 0; i < 7 && i < wrapItems.Count; i++)
                                    wrapItems[i] = itemToSlot ?? "";
                            }
                            break;
                    }
                    changed = true;
                    break;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            var lockerSlots = (McpCommon.Item(profile, lockerItem!)?["attributes"] as JsonObject)
                ?.GetObj("locker_slots_data");
            changes.Add(McpCommon.ItemAttrChanged(lockerItem!, "locker_slots_data", lockerSlots));
            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }
}
