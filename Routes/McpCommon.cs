using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;


public static class McpCommon
{
    
    public static readonly SemaphoreSlim Gate = new(8, 8);

    private static JsonObject _catalog;
    public static JsonObject Catalog => _catalog ??= Functions.GetItemShop();

    public static string ProfileId(HttpContext ctx, string fallback) =>
        RequestUtil.Query(ctx, "profileId") is { Length: > 0 } id ? id : fallback;

    public static long QueryRevision(HttpContext ctx)
    {
        var value = RequestUtil.Query(ctx, "rvn");
        return long.TryParse(value, out var rvn) ? rvn : -1;
    }

    public static JsonObject Profile(string profileId) => ProfileStore.Get(profileId);
    public static void Save(string profileId) => ProfileStore.Save(profileId);
    public static void Save(string profileId, JsonObject profile) => ProfileStore.Save(profileId, profile);

    public static JsonObject Body(HttpContext ctx) => ctx.Items["body"] as JsonObject ?? new JsonObject();

    public static JsonObject Attrs(JsonObject profile)
    {
        if (profile["stats"] is not JsonObject stats) { stats = new JsonObject(); profile["stats"] = stats; }
        if (stats["attributes"] is not JsonObject attrs) { attrs = new JsonObject(); stats["attributes"] = attrs; }
        return attrs;
    }

    public static JsonObject Items(JsonObject profile)
    {
        if (profile["items"] is not JsonObject items) { items = new JsonObject(); profile["items"] = items; }
        return items;
    }

    public static JsonObject Item(JsonObject profile, string itemId) =>
        Items(profile)[itemId] as JsonObject;

    public static JsonObject NewItem(string templateId, JsonObject attributes, long quantity = 1) => new()
    {
        ["templateId"] = templateId,
        ["attributes"] = attributes,
        ["quantity"] = quantity
    };

    public static void Increment(JsonObject profile)
    {
        profile["rvn"] = profile.GetLong("rvn") + 1;
        profile["commandRevision"] = profile.GetLong("commandRevision") + 1;
    }

    public static JsonObject Change(string changeType) => new() { ["changeType"] = changeType };

    public static JsonObject StatModified(string name, JsonNode value) => new()
    {
        ["changeType"] = "statModified",
        ["name"] = name,
        ["value"] = value?.DeepClone()
    };

    public static JsonObject ItemAdded(string itemId, JsonNode item) => new()
    {
        ["changeType"] = "itemAdded",
        ["itemId"] = itemId,
        ["item"] = item?.DeepClone()
    };

    public static JsonObject ItemRemoved(string itemId) => new()
    {
        ["changeType"] = "itemRemoved",
        ["itemId"] = itemId
    };

    public static JsonObject ItemAttrChanged(string itemId, string attributeName, JsonNode value) => new()
    {
        ["changeType"] = "itemAttrChanged",
        ["itemId"] = itemId,
        ["attributeName"] = attributeName,
        ["attributeValue"] = value?.DeepClone()
    };

    public static JsonObject ItemQuantityChanged(string itemId, long quantity) => new()
    {
        ["changeType"] = "itemQuantityChanged",
        ["itemId"] = itemId,
        ["quantity"] = quantity
    };

    public static JsonObject FullUpdate(JsonObject profile) => new()
    {
        ["changeType"] = "fullProfileUpdate",
        ["profile"] = profile?.DeepClone()
    };

    public static async Task Finish(
        HttpContext ctx,
        string profileId,
        JsonObject profile,
        long baseRevision,
        JsonArray changes,
        JsonArray multiUpdate = null,
        JsonArray notifications = null,
        bool applyFallback = true)
    {
        if (applyFallback && QueryRevision(ctx) != baseRevision)
            changes = new JsonArray { FullUpdate(profile) };

        var response = new JsonObject
        {
            ["profileRevision"] = profile.GetLong("rvn"),
            ["profileId"] = profileId,
            ["profileChangesBaseRevision"] = baseRevision,
            ["profileChanges"] = changes ?? new JsonArray(),
            ["profileCommandRevision"] = profile.GetLong("commandRevision"),
            ["serverTime"] = Functions.IsoNow(),
            ["responseVersion"] = 1
        };

        if (multiUpdate != null) response["multiUpdate"] = multiUpdate;
        if (notifications != null) response["notifications"] = notifications;

        await ResponseUtil.Json(ctx, response);
    }

    public static JsonArray AsArray(JsonNode node) => node as JsonArray ?? new JsonArray();
}

