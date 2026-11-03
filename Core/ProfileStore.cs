using System.Collections.Concurrent;
using System.Text.Json.Nodes;

namespace LawinServer.Core;




public static class ProfileStore
{
    private static readonly ConcurrentDictionary<string, JsonObject> Profiles =
        new(StringComparer.OrdinalIgnoreCase);

    private static readonly object Gate = new();

    public static void LoadAll()
    {
        Profiles.Clear();
        if (!Directory.Exists(Paths.ProfilesDir)) return;

        foreach (var file in Directory.GetFiles(Paths.ProfilesDir, "*.json"))
        {
            var id = Path.GetFileNameWithoutExtension(file);
            var text = File.ReadAllText(file);
            if (J.Parse(text) is not JsonObject obj) obj = new JsonObject();
            Normalize(obj);
            Profiles[id] = obj;
        }
    }

    public static void Normalize(JsonObject profile)
    {
        if (!profile.ContainsKey("rvn")) profile["rvn"] = 0;
        if (!profile.ContainsKey("items")) profile["items"] = new JsonObject();
        if (!profile.ContainsKey("stats")) profile["stats"] = new JsonObject();
        var stats = profile["stats"] as JsonObject;
        if (stats != null && !stats.ContainsKey("attributes")) stats["attributes"] = new JsonObject();
        if (!profile.ContainsKey("commandRevision")) profile["commandRevision"] = 0;
    }

    public static bool Exists(string profileId) => Profiles.ContainsKey(profileId);

    public static JsonObject Get(string profileId)
    {
        if (string.IsNullOrEmpty(profileId)) profileId = "athena";
        if (Profiles.TryGetValue(profileId, out var p)) return p;

        lock (Gate)
        {
            if (Profiles.TryGetValue(profileId, out p)) return p;
            var file = Path.Combine(Paths.ProfilesDir, profileId + ".json");
            JsonObject obj;
            if (File.Exists(file))
            {
                obj = J.Parse(File.ReadAllText(file)) as JsonObject ?? new JsonObject();
            }
            else
            {
                obj = new JsonObject
                {
                    ["profileId"] = profileId,
                    ["rvn"] = 0,
                    ["commandRevision"] = 0,
                    ["items"] = new JsonObject(),
                    ["stats"] = new JsonObject { ["attributes"] = new JsonObject() }
                };
            }
            Normalize(obj);
            Profiles[profileId] = obj;
            return obj;
        }
    }

    public static void Save(string profileId)
    {
        var profile = Get(profileId);
        Save(profileId, profile);
    }

    public static void Save(string profileId, JsonObject profile)
    {
        lock (Gate)
        {
            Directory.CreateDirectory(Paths.ProfilesDir);
            Profiles[profileId] = profile;
        }
        var path = Path.Combine(Paths.ProfilesDir, profileId + ".json");
        var snapshot = profile.DeepClone();
        _ = Task.Run(() =>
        {
            try
            {
                lock (Gate) { File.WriteAllText(path, snapshot.ToJsonString(J.Compact)); }
            }
            catch { }
        });
    }

    public static IEnumerable<string> Ids => Profiles.Keys;
}

