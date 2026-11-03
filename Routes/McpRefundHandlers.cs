using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpRefundHandlers
{
    public static async Task RefundItem(HttpContext ctx)
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
            var item = McpCommon.Item(profile, targetItemId);
            if (item != null)
            {
                var templateId = item.GetStr("templateId") ?? "";
                item["templateId"] = System.Text.RegularExpressions.Regex.Replace(templateId, "\\d$", "") + "1";

                if (item["attributes"] is not JsonObject attrs)
                {
                    attrs = new JsonObject();
                    item["attributes"] = attrs;
                }

                attrs["level"] = 1;
                attrs["refundable"] = false;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);

            var id = Functions.MakeId();
            var items = McpCommon.Items(profile);
            items[id] = items[targetItemId]?.DeepClone();
            changes.Add(McpCommon.ItemAdded(id, items[id]));

            items.Remove(targetItemId);
            changes.Add(McpCommon.ItemRemoved(targetItemId));

            McpCommon.Save(profileId, profile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes);
    }

    public static async Task RefundMtxPurchase(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "common_core");
        var profile = McpCommon.Profile(profileId);
        var itemProfile = McpCommon.Profile("athena");

        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;
        var itemGuids = new List<string>();

        var purchaseId = body.GetStr("purchaseId");
        if (!string.IsNullOrEmpty(purchaseId))
        {
            var itemProfileUpdate = new JsonObject
            {
                ["profileRevision"] = itemProfile.GetLong("rvn"),
                ["profileId"] = "athena",
                ["profileChangesBaseRevision"] = itemProfile.GetLong("rvn"),
                ["profileChanges"] = new JsonArray(),
                ["profileCommandRevision"] = itemProfile.GetLong("commandRevision")
            };
            multiUpdate.Add(itemProfileUpdate);

            var attrs = McpCommon.Attrs(profile);
            var purchaseHistory = attrs["mtx_purchase_history"] as JsonObject;
            if (purchaseHistory != null)
            {
                purchaseHistory["refundsUsed"] = purchaseHistory.GetLong("refundsUsed") + 1;
                purchaseHistory["refundCredits"] = purchaseHistory.GetLong("refundCredits") - 1;

                var currentPlatform = attrs.GetStr("current_mtx_platform") ?? "";

                if (purchaseHistory["purchases"] is JsonArray purchases)
                {
                    foreach (var purchaseNode in purchases)
                    {
                        if (purchaseNode is not JsonObject purchase) continue;
                        if (purchase.GetStr("purchaseId") != purchaseId) continue;

                        if (purchase["lootResult"] is JsonArray lootResult)
                        {
                            foreach (var lootNode in lootResult)
                            {
                                var guid = (lootNode as JsonObject)?.GetStr("itemGuid");
                                if (!string.IsNullOrEmpty(guid)) itemGuids.Add(guid);
                            }
                        }

                        purchase["refundDate"] = Functions.IsoNow();

                        var items = McpCommon.Items(profile);
                        foreach (var key in items.Select(kv => kv.Key).ToList())
                        {
                            var entry = items[key] as JsonObject;
                            if (!(entry?.GetStr("templateId") ?? "").ToLowerInvariant().StartsWith("currency:mtx")) continue;

                            var platform = (entry["attributes"] as JsonObject)?.GetStr("platform")?.ToLowerInvariant() ?? "";
                            if (platform == currentPlatform.ToLowerInvariant() || platform == "shared")
                            {
                                entry["quantity"] = entry.GetLong("quantity") + purchase.GetLong("totalMtxPaid");
                                changes.Add(McpCommon.ItemQuantityChanged(key, entry.GetLong("quantity")));
                                break;
                            }
                        }
                    }
                }
            }

            foreach (var guid in itemGuids)
            {
                try
                {
                    var itemItems = McpCommon.Items(itemProfile);
                    itemItems.Remove(guid);
                    ((JsonArray)itemProfileUpdate["profileChanges"]!).Add(McpCommon.ItemRemoved(guid));
                }
                catch
                {
                    
                }
            }

            itemProfile["rvn"] = itemProfile.GetLong("rvn") + 1;
            itemProfile["commandRevision"] = itemProfile.GetLong("commandRevision") + 1;
            profile["rvn"] = profile.GetLong("rvn") + 1;
            profile["commandRevision"] = profile.GetLong("commandRevision") + 1;

            changed = true;
        }

        if (changed)
        {
            var attrs = McpCommon.Attrs(profile);
            changes.Add(McpCommon.StatModified("mtx_purchase_history", attrs["mtx_purchase_history"]));

            var itemProfileUpdate = (JsonObject)multiUpdate[0]!;
            itemProfileUpdate["profileRevision"] = itemProfile.GetLong("rvn");
            itemProfileUpdate["profileCommandRevision"] = itemProfile.GetLong("commandRevision");

            McpCommon.Save(profileId, profile);
            McpCommon.Save("athena", itemProfile);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes, multiUpdate: multiUpdate);
    }
}
