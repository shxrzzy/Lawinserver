using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class CloudStorageRoutes
{
    private static string SettingsDir()
    {
        var local = Environment.GetEnvironmentVariable("LOCALAPPDATA");
        if (!string.IsNullOrEmpty(local))
        {
            var dir = Path.Combine(local, "LawinServer", "ClientSettings");
            Directory.CreateDirectory(dir);
            return dir;
        }
        var fallback = Paths.Combine("ClientSettings");
        Directory.CreateDirectory(fallback);
        return fallback;
    }

    private static string SettingsFile(string buildId) =>
        Path.Combine(SettingsDir(), $"ClientSettings-{buildId}.Sav");

    public static void Map(WebApplication app)
    {
        app.MapGet("/fortnite/api/cloudstorage/system", async (HttpContext ctx) =>
        {
            var memory = Functions.GetVersionInfo(ctx);
            if (memory.Build >= 9.40 && memory.Build <= 10.40)
            {
                await ResponseUtil.Empty(ctx, 404);
                return;
            }

            var files = new JsonArray();
            foreach (var file in Directory.GetFiles(Paths.CloudStorageDir))
            {
                var name = Path.GetFileName(file);
                if (!name.EndsWith(".ini", StringComparison.OrdinalIgnoreCase)) continue;

                var text = await File.ReadAllTextAsync(file);
                var info = new FileInfo(file);
                files.Add(new JsonObject
                {
                    ["uniqueFilename"] = name,
                    ["filename"] = name,
                    ["hash"] = Sha1(text),
                    ["hash256"] = Sha256(text),
                    ["length"] = text.Length,
                    ["contentType"] = "application/octet-stream",
                    ["uploaded"] = Functions.Iso(info.LastWriteTimeUtc),
                    ["storageType"] = "S3",
                    ["storageIds"] = new JsonObject(),
                    ["doNotCache"] = true
                });
            }

            await ResponseUtil.Json(ctx, files);
        });

        app.MapGet("/fortnite/api/cloudstorage/system/{file}", async (HttpContext ctx) =>
        {
            var memory = Functions.GetVersionInfo(ctx);
            var name = RequestUtil.Route(ctx, "file") ?? "";
            var file = Path.Combine(Paths.CloudStorageDir, name);

            if (File.Exists(file))
            {
                var text = await File.ReadAllTextAsync(file);

                if (name == "DefaultEngine.ini" && memory.Season >= 23)
                    text += "\n[ConsoleVariables]\nnet.AllowEncryption=0\n";

                if (name == "DefaultRuntimeOptions.ini" && 17.50 <= memory.Build && memory.Build <= 19.30)
                    text += "\nbLoadDirectlyIntoLobby=false\n";

                await ResponseUtil.Text(ctx, text);
                return;
            }

            await ResponseUtil.Empty(ctx, 200);
        });

        app.MapGet("/fortnite/api/cloudstorage/user/{accountId}/{file}", async (HttpContext ctx) =>
        {
            var name = RequestUtil.Route(ctx, "file") ?? "";
            ctx.Response.ContentType = "application/octet-stream";

            if (!name.Equals("clientsettings.sav", StringComparison.OrdinalIgnoreCase))
            {
                await ResponseUtil.Json(ctx, new JsonObject { ["error"] = "file not found" }, 404);
                return;
            }

            var memory = Functions.GetVersionInfo(ctx);
            var file = SettingsFile(memory.CL);

            if (File.Exists(file))
            {
                var data = await File.ReadAllBytesAsync(file);
                await ResponseUtil.Bytes(ctx, data);
            }
            else
            {
                await ResponseUtil.Empty(ctx, 200);
            }
        });

        app.MapGet("/fortnite/api/cloudstorage/user/{accountId}", async (HttpContext ctx) =>
        {
            ctx.Response.ContentType = "application/json";
            var memory = Functions.GetVersionInfo(ctx);
            var file = SettingsFile(memory.CL);

            if (File.Exists(file))
            {
                var text = await File.ReadAllTextAsync(file, Encoding.Latin1);
                var info = new FileInfo(file);
                await ResponseUtil.Json(ctx, new JsonArray
                {
                    new JsonObject
                    {
                        ["uniqueFilename"] = "ClientSettings.Sav",
                        ["filename"] = "ClientSettings.Sav",
                        ["hash"] = Sha1(text),
                        ["hash256"] = Sha256(text),
                        ["length"] = Encoding.Latin1.GetByteCount(text),
                        ["contentType"] = "application/octet-stream",
                        ["uploaded"] = Functions.Iso(info.LastWriteTimeUtc),
                        ["storageType"] = "S3",
                        ["storageIds"] = new JsonObject(),
                        ["accountId"] = RequestUtil.Route(ctx, "accountId"),
                        ["doNotCache"] = true
                    }
                });
            }
            else
            {
                await ResponseUtil.Json(ctx, new JsonArray());
            }
        });

        app.MapPut("/fortnite/api/cloudstorage/user/{accountId}/{file}", async (HttpContext ctx) =>
        {
            var name = RequestUtil.Route(ctx, "file") ?? "";
            if (!name.Equals("clientsettings.sav", StringComparison.OrdinalIgnoreCase))
            {
                await ResponseUtil.Json(ctx, new JsonObject { ["error"] = "file not found" }, 404);
                return;
            }

            var memory = Functions.GetVersionInfo(ctx);
            var raw = await RequestUtil.ReadRawLatin1Async(ctx);
            await File.WriteAllBytesAsync(SettingsFile(memory.CL), Encoding.Latin1.GetBytes(raw));
            await ResponseUtil.Empty(ctx, 204);
        });
    }

    private static string Sha1(string text) =>
        Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();

    private static string Sha256(string text) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text))).ToLowerInvariant();
}
