using System.Globalization;

namespace LawinServer.Core;





public static class AppConfig
{
    private static readonly Dictionary<string, Dictionary<string, string>> Sections =
        new(StringComparer.OrdinalIgnoreCase);

    public static void Load(string file = null)
    {
        Sections.Clear();
        file ??= Paths.ConfigFile;
        if (!File.Exists(file)) return;

        string current = "DEFAULT";
        Sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var rawLine in File.ReadAllLines(file))
        {
            var line = rawLine.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                current = line[1..^1].Trim();
                if (!Sections.ContainsKey(current))
                    Sections[current] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                continue;
            }

            var idx = line.IndexOf('=');
            if (idx < 0) continue;
            var key = line[..idx].Trim();
            var value = line[(idx + 1)..].Trim();
            Sections[current][key] = value;
        }
    }

    public static string Get(string section, string key, string fallback = null)
    {
        if (Sections.TryGetValue(section, out var s) && s.TryGetValue(key, out var v)) return v;
        return fallback;
    }

    public static bool GetBool(string section, string key, bool fallback = false)
    {
        var v = Get(section, key);
        if (v == null) return fallback;
        return v.Equals("true", StringComparison.OrdinalIgnoreCase) || v == "1";
    }

    public static int GetInt(string section, string key, int fallback = 0)
    {
        var v = Get(section, key);
        return int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    }

    public static double GetDouble(string section, string key, double fallback = 0)
    {
        var v = Get(section, key);
        return double.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out var n) ? n : fallback;
    }
}
