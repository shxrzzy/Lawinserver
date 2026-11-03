using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;

namespace LawinServer.Core;




public static class J
{
    public static readonly JsonSerializerOptions Compact = new()
    {
        WriteIndented = false,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static readonly JsonSerializerOptions Pretty = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver()
    };

    public static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip
    };

    public static JsonNode Parse(string text) =>
        string.IsNullOrWhiteSpace(text) ? null : JsonNode.Parse(text, null, DocumentOptions);

    public static JsonObject Obj() => new();

    public static JsonArray Arr() => new();

    public static JsonObject Obj(JsonNode node) => node as JsonObject;

    public static JsonArray Arr(JsonNode node) => node as JsonArray;

    public static JsonNode Clone(JsonNode node) => node?.DeepClone();

    public static string Json(this JsonNode node, bool pretty = false) =>
        node == null ? "null" : node.ToJsonString(pretty ? Pretty : Compact);

    public static JsonNode From(object value) => JsonSerializer.SerializeToNode(value, Compact);

    public static long AsLong(JsonNode node, long fallback = 0)
    {
        if (node == null) return fallback;
        try
        {
            if (node is JsonValue v)
            {
                if (v.TryGetValue<long>(out var l)) return l;
                if (v.TryGetValue<int>(out var i)) return i;
                if (v.TryGetValue<double>(out var d)) return (long)d;
                if (v.TryGetValue<decimal>(out var m)) return (long)m;
                if (v.TryGetValue<string>(out var s) && long.TryParse(s, out var ps)) return ps;
            }
        }
        catch { }
        return fallback;
    }

    public static double AsDouble(JsonNode node, double fallback = 0)
    {
        if (node == null) return fallback;
        try
        {
            if (node is JsonValue v)
            {
                if (v.TryGetValue<double>(out var d)) return d;
                if (v.TryGetValue<long>(out var l)) return l;
                if (v.TryGetValue<int>(out var i)) return i;
                if (v.TryGetValue<string>(out var s) && double.TryParse(s, out var ps)) return ps;
            }
        }
        catch { }
        return fallback;
    }

    public static bool AsBool(JsonNode node, bool fallback = false)
    {
        if (node == null) return fallback;
        try
        {
            if (node is JsonValue v)
            {
                if (v.TryGetValue<bool>(out var b)) return b;
                if (v.TryGetValue<string>(out var s)) return s.Equals("true", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }
        return fallback;
    }

    public static string AsString(JsonNode node, string fallback = null)
    {
        if (node == null) return fallback;
        try
        {
            if (node is JsonValue v)
            {
                if (v.TryGetValue<string>(out var s)) return s;
                return node.ToJsonString(Compact).Trim('"');
            }
        }
        catch { }
        return fallback;
    }
}

public static class JsonNodeExtensions
{
    public static bool Has(this JsonNode node, string key) =>
        node is JsonObject o && o.ContainsKey(key);

    public static JsonNode Get(this JsonNode node, string key) =>
        node is JsonObject o && o.TryGetPropertyValue(key, out var v) ? v : null;

    public static JsonObject GetObj(this JsonNode node, string key) => node.Get(key) as JsonObject;
    public static JsonArray GetArr(this JsonNode node, string key) => node.Get(key) as JsonArray;
    public static string GetStr(this JsonNode node, string key, string fallback = null) => J.AsString(node.Get(key), fallback);
    public static long GetLong(this JsonNode node, string key, long fallback = 0) => J.AsLong(node.Get(key), fallback);
    public static double GetDouble(this JsonNode node, string key, double fallback = 0) => J.AsDouble(node.Get(key), fallback);
    public static bool GetBool(this JsonNode node, string key, bool fallback = false) => J.AsBool(node.Get(key), fallback);

    public static JsonNode Set(this JsonNode node, string key, JsonNode value)
    {
        if (node is JsonObject o) o[key] = value;
        return node;
    }

    public static JsonNode Set(this JsonNode node, string key, string value) => node.Set(key, JsonValue.Create(value));
    public static JsonNode Set(this JsonNode node, string key, long value) => node.Set(key, JsonValue.Create(value));
    public static JsonNode Set(this JsonNode node, string key, int value) => node.Set(key, JsonValue.Create(value));
    public static JsonNode Set(this JsonNode node, string key, double value) => node.Set(key, JsonValue.Create(value));
    public static JsonNode Set(this JsonNode node, string key, bool value) => node.Set(key, JsonValue.Create(value));

    public static void Remove(this JsonNode node, string key)
    {
        if (node is JsonObject o) o.Remove(key);
    }

    public static JsonObject DeepObj(this JsonNode node) => (JsonObject)node.DeepClone();
    public static JsonArray DeepArr(this JsonNode node) => (JsonArray)node.DeepClone();
}
