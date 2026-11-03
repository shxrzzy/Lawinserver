using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpCollectionHandlers
{
    public static async Task ResearchItemFromCollectionBook(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var id = Functions.MakeId();
        var templateId = body.GetStr("templateId");
        if (!string.IsNullOrEmpty(templateId))
        {
            McpCommon.Items(profile)[id] = McpCommon.NewItem(templateId, new JsonObject
            {
                ["last_state_change_time"] = "2017-08-29T21:05:57.087Z",
                ["max_level_bonus"] = 0,
                ["level"] = 1,
                ["item_seen"] = false,
                ["xp"] = 0,
                ["sent_new_notification"] = true,
                ["favorite"] = false
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

    public static async Task SlotItemInCollectionBook(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var itemId = body.GetStr("itemId");
        var item = string.IsNullOrEmpty(itemId) ? null : McpCommon.Item(profile, itemId);
        var templateId = item?.GetStr("templateId") ?? "";

        var bookId = templateId.ToLowerInvariant().StartsWith("schematic:")
            ? "collection_book_schematics0"
            : "collection_book_people0";
        var book = McpCommon.Profile(bookId);

        if (!string.IsNullOrEmpty(itemId) && item != null)
        {
            var bookUpdate = new JsonObject
            {
                ["profileRevision"] = book.GetLong("rvn"),
                ["profileId"] = book.GetStr("profileId", bookId),
                ["profileChangesBaseRevision"] = book.GetLong("rvn"),
                ["profileChanges"] = new JsonArray(),
                ["profileCommandRevision"] = book.GetLong("commandRevision")
            };
            multiUpdate.Add(bookUpdate);

            var bookItems = McpCommon.Items(book);
            var itemTemplate = item.GetStr("templateId") ?? "";
            foreach (var key in bookItems.Select(kv => kv.Key).ToList())
            {
                var bookItem = bookItems[key] as JsonObject;
                var bookTemplate = bookItem?.GetStr("templateId") ?? "";

                if (Prefix(itemTemplate) != Prefix(bookTemplate)) continue;

                if (itemTemplate.ToLowerInvariant().StartsWith("worker:") &&
                    bookTemplate.ToLowerInvariant().StartsWith("worker:"))
                {
                    var personality1 = (item["attributes"] as JsonObject)?.GetStr("personality");
                    var personality2 = (bookItem?["attributes"] as JsonObject)?.GetStr("personality");
                    if (personality1 != null && personality2 != null &&
                        personality1.ToLowerInvariant() == personality2.ToLowerInvariant())
                    {
                        bookItems.Remove(key);
                        ((JsonArray)bookUpdate["profileChanges"]!).Add(McpCommon.ItemRemoved(key));
                    }
                }
                else
                {
                    bookItems.Remove(key);
                    ((JsonArray)bookUpdate["profileChanges"]!).Add(McpCommon.ItemRemoved(key));
                }
            }

            bookItems[itemId] = item.DeepClone();
            McpCommon.Items(profile).Remove(itemId);

            changed = true;
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Increment(book);

            var bookUpdate = (JsonObject)multiUpdate[0]!;
            bookUpdate["profileRevision"] = book.GetLong("rvn");
            bookUpdate["profileCommandRevision"] = book.GetLong("commandRevision");

            changes.Add(McpCommon.ItemRemoved(itemId!));
            ((JsonArray)bookUpdate["profileChanges"]!).Add(McpCommon.ItemAdded(itemId!, McpCommon.Item(book, itemId!)));

            notifications.Add(new JsonObject
            {
                ["type"] = "slotItemResult",
                ["primary"] = true,
                ["slottedItemId"] = itemId
            });

            McpCommon.Save(profileId, profile);
            McpCommon.Save(bookId, book);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            multiUpdate: multiUpdate, notifications: notifications);
    }

    public static async Task UnslotItemFromCollectionBook(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var multiUpdate = new JsonArray();
        var notifications = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        var templateId = body.GetStr("templateId") ?? "";
        var bookId = templateId.ToLowerInvariant().StartsWith("schematic:")
            ? "collection_book_schematics0"
            : "collection_book_people0";
        var book = McpCommon.Profile(bookId);

        var bookUpdate = new JsonObject
        {
            ["profileRevision"] = book.GetLong("rvn"),
            ["profileId"] = book.GetStr("profileId", bookId),
            ["profileChangesBaseRevision"] = book.GetLong("rvn"),
            ["profileChanges"] = new JsonArray(),
            ["profileCommandRevision"] = book.GetLong("commandRevision")
        };
        multiUpdate.Add(bookUpdate);

        var itemId = body.GetStr("itemId");
        var id = Functions.MakeId();
        var items = McpCommon.Items(profile);
        var bookItems = McpCommon.Items(book);

        if (!string.IsNullOrEmpty(itemId))
        {
            if (items.ContainsKey(itemId))
            {
                items[id] = bookItems[itemId]?.DeepClone();
                changes.Add(McpCommon.ItemAdded(id, items[id]));

                bookItems.Remove(itemId);
                ((JsonArray)bookUpdate["profileChanges"]!).Add(McpCommon.ItemRemoved(itemId));

                changed = true;
            }
            else
            {
                items[itemId] = bookItems[itemId]?.DeepClone();
                changes.Add(McpCommon.ItemAdded(itemId, items[itemId]));

                bookItems.Remove(itemId);
                ((JsonArray)bookUpdate["profileChanges"]!).Add(McpCommon.ItemRemoved(itemId));

                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            McpCommon.Increment(book);

            bookUpdate["profileRevision"] = book.GetLong("rvn");
            bookUpdate["profileCommandRevision"] = book.GetLong("commandRevision");

            McpCommon.Save(profileId, profile);
            McpCommon.Save(bookId, book);
        }

        await McpCommon.Finish(ctx, profileId, profile, baseRevision, changes,
            multiUpdate: multiUpdate, notifications: notifications);
    }

    public static async Task ClaimCollectionBookRewards(HttpContext ctx)
    {
        var body = McpCommon.Body(ctx);
        var profileId = McpCommon.ProfileId(ctx, "campaign");
        var profile = McpCommon.Profile(profileId);
        var changes = new JsonArray();
        var baseRevision = profile.GetLong("rvn");
        var changed = false;

        if (body.GetLong("requiredXp") != 0)
        {
            var attrs = McpCommon.Attrs(profile);
            if (attrs["collection_book"] is JsonObject collectionBook)
            {
                collectionBook["maxBookXpLevelAchieved"] = collectionBook.GetLong("maxBookXpLevelAchieved") + 1;
                changed = true;
            }
        }

        if (changed)
        {
            McpCommon.Increment(profile);
            changes.Add(McpCommon.StatModified("collection_book", McpCommon.Attrs(profile)["collection_book"]));
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
