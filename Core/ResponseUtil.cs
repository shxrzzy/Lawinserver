using System.Text;
using System.Text.Json.Nodes;

namespace LawinServer.Core;

public static class ResponseUtil
{
    public static async Task Json(HttpContext ctx, JsonNode node, int status = 200)
    {
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = "application/json; charset=utf-8";
        await ctx.Response.WriteAsync(node?.ToJsonString(J.Compact) ?? "null");
    }

    public static async Task Json(HttpContext ctx, object value, int status = 200)
    {
        await Json(ctx, J.From(value), status);
    }

    public static Task Empty(HttpContext ctx, int status = 204)
    {
        ctx.Response.StatusCode = status;
        return Task.CompletedTask;
    }

    public static async Task Text(HttpContext ctx, string text, string contentType = "text/plain")
    {
        ctx.Response.ContentType = contentType;
        await ctx.Response.WriteAsync(text);
    }

    public static async Task Bytes(HttpContext ctx, byte[] data, string contentType = "application/octet-stream")
    {
        ctx.Response.ContentType = contentType;
        await ctx.Response.Body.WriteAsync(data);
    }

    public static void SetEpicError(HttpContext ctx, string name, int code)
    {
        ctx.Response.Headers["X-Epic-Error-Name"] = name;
        ctx.Response.Headers["X-Epic-Error-Code"] = code.ToString();
    }

    public static async Task NotFoundEpic(HttpContext ctx)
    {
        SetEpicError(ctx, "errors.com.lawinserver.common.not_found", 1004);
        await Json(ctx, new JsonObject
        {
            ["errorCode"] = "errors.com.lawinserver.common.not_found",
            ["errorMessage"] = "Sorry the resource you were trying to find could not be found",
            ["numericErrorCode"] = 1004,
            ["originatingService"] = "any",
            ["intent"] = "prod"
        }, 404);
    }
}
