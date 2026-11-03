using System.Globalization;
using System.Text.Json.Nodes;

namespace LawinServer.Core;

public static class RequestUtil
{
    public static string Query(HttpContext ctx, string name)
    {
        var values = ctx.Request.Query[name];
        return values.Count > 0 ? values[0] : null;
    }

    public static IEnumerable<string> QueryAll(HttpContext ctx, string name) => ctx.Request.Query[name];

    public static string Route(HttpContext ctx, string name) =>
        ctx.Request.RouteValues.TryGetValue(name, out var v) ? v?.ToString() : null;

    public static string Header(HttpContext ctx, string name) => ctx.Request.Headers[name].ToString();

    public static string UserAgent(HttpContext ctx) => ctx.Request.Headers.UserAgent.ToString();

    public static string AcceptLanguage(HttpContext ctx) => ctx.Request.Headers.AcceptLanguage.ToString();

    public static async Task<string> ReadRawAsync(HttpContext ctx)
    {
        using var reader = new StreamReader(ctx.Request.Body, System.Text.Encoding.UTF8, true, 4096, leaveOpen: true);
        return await reader.ReadToEndAsync();
    }

    public static async Task<string> ReadRawLatin1Async(HttpContext ctx)
    {
        using var ms = new MemoryStream();
        await ctx.Request.Body.CopyToAsync(ms);
        return System.Text.Encoding.Latin1.GetString(ms.ToArray());
    }

    public static async Task<JsonNode> ReadBodyAsync(HttpContext ctx)
    {
        var contentType = ctx.Request.ContentType ?? "";
        if (contentType.Contains("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
            var form = await ctx.Request.ReadFormAsync();
            var obj = new JsonObject();
            foreach (var pair in form)
                obj[pair.Key] = pair.Value.Count == 1 ? pair.Value[0] : new JsonArray(pair.Value.Select(v => (JsonNode)v).ToArray());
            return obj;
        }

        var raw = await ReadRawAsync(ctx);
        if (string.IsNullOrWhiteSpace(raw)) return new JsonObject();
        try
        {
            return J.Parse(raw) ?? new JsonObject();
        }
        catch
        {
            return new JsonObject();
        }
    }

    public static async Task<JsonObject> ReadObjectAsync(HttpContext ctx)
    {
        var body = await ReadBodyAsync(ctx);
        return body as JsonObject ?? new JsonObject();
    }

    public static async Task<JsonArray> ReadArrayAsync(HttpContext ctx)
    {
        var body = await ReadBodyAsync(ctx);
        return body as JsonArray ?? new JsonArray();
    }
}
