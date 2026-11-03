using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpModularLoadoutHandler
{
    public static async Task PutModularCosmeticLoadout(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "athena");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var loadoutData = body["loadoutData"] is JsonValue value && value.TryGetValue<string>(out var raw)
            ? J.Parse(raw) as JsonObject ?? new JsonObject()
            : body["loadoutData"] as JsonObject ?? new JsonObject();

        var attrs = McpCommon.Attrs(profile);
        var items = McpCommon.Items(profile);

        if (attrs["loadout_presets"] is not JsonObject loadoutPresets)
        {
            loadoutPresets = new JsonObject();
            attrs["loadout_presets"] = loadoutPresets;

            changes.Add(McpCommon.StatModified("loadout_presets", new JsonObject()));
            changed = true;
        }

        var loadoutType = body.GetStr("loadoutType");
        var presetId = body.GetStr("presetId");

        if (loadoutType != null && !loadoutPresets.ContainsKey(loadoutType))
        {
            var newLoadoutId = Functions.MakeId();
            items[newLoadoutId] = McpCommon.NewItem(loadoutType, new JsonObject());

            changes.Add(McpCommon.ItemAdded(newLoadoutId, items[newLoadoutId]));

            loadoutPresets[loadoutType] = new JsonObject { [presetId ?? ""] = newLoadoutId };
            changes.Add(McpCommon.StatModified("loadout_presets", loadoutPresets));
            changed = true;
        }

        try
        {
            var loadoutGuid = (loadoutPresets[loadoutType ?? ""] as JsonObject)?[presetId ?? ""] is JsonValue guidValue
                ? J.AsString(guidValue)
                : null;

            if (loadoutGuid != null && items[loadoutGuid] is JsonObject loadoutItem)
            {
                loadoutItem["attributes"] = loadoutData.DeepClone();

                changes.Add(McpCommon.ItemAttrChanged(loadoutGuid, "slots",
                    (loadoutItem["attributes"] as JsonObject)?["slots"]));
                changed = true;
            }
        }
        catch
        {
            
        }

        
        if (loadoutData["slots"] is JsonArray slots)
        {
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

                    changes.Add(McpCommon.ItemAttrChanged(key, "variants", variants));
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
}
