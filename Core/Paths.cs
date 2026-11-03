namespace LawinServer.Core;





public static class Paths
{
    public static string Root { get; set; } = Directory.GetCurrentDirectory();

    public static string Combine(params string[] parts) => Path.Combine(new[] { Root }.Concat(parts).ToArray());

    public static string ConfigFile => Combine("Config", "config.ini");
    public static string CatalogConfigFile => Combine("Config", "catalog_config.json");
    public static string ProfilesDir => Combine("profiles");
    public static string ResponsesDir => Combine("responses");
    public static string CloudStorageDir => Combine("CloudStorage");
    public static string PublicDir => Combine("public");

    public static string Response(string relative) => Combine("responses", relative);
}
