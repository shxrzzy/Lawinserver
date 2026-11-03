using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace LawinServer.Core;





public static class DataStore
{
    private static readonly ConcurrentDictionary<string, JsonNode> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Gate = new();

    public static JsonNode Get(string relativePath)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : Paths.Combine(relativePath);
        if (Cache.TryGetValue(full, out var cached)) return cached;

        lock (Gate)
        {
            if (Cache.TryGetValue(full, out cached)) return cached;
            if (!File.Exists(full)) return null;
            var text = File.ReadAllText(full);
            var node = J.Parse(text);
            Cache[full] = node;
            return node;
        }
    }

    
    public static JsonNode Response(string relative) => Get(Paths.Combine("responses", relative));

    public static JsonNode CatalogConfig() => Get(Paths.CatalogConfigFile);

    public static void Save(string relativePath, JsonNode node)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : Paths.Combine(relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        lock (Gate)
        {
            Cache[full] = node;
        }
        var snapshot = node?.DeepClone();
        _ = Task.Run(() =>
        {
            try
            {
                lock (Gate) { File.WriteAllText(full, snapshot?.ToJsonString(J.Compact) ?? "null"); }
            }
            catch { }
        });
    }

    public static void SaveResponse(string relative, JsonNode node) =>
        Save(Paths.Combine("responses", relative), node);

    public static void Reload(string relativePath)
    {
        var full = Path.IsPathRooted(relativePath) ? relativePath : Paths.Combine(relativePath);
        Cache.TryRemove(full, out _);
    }
}

