using System.Text.Json.Nodes;
using LawinServer.Core;

namespace LawinServer.Routes;

public static class UserRoutes
{
    private static string MemoryCurrentAccountId =
        AppConfig.Get("Config", "displayName", "LawinServer");

    public static void Map(WebApplication app)
    {
        app.MapGet("/account/api/public/account", async (HttpContext ctx) =>
        {
            var response = new JsonArray();
            var ids = RequestUtil.QueryAll(ctx, "accountId").ToList();

            foreach (var raw in ids)
            {
                var accountId = raw;
                if (accountId.Contains('@')) accountId = accountId.Split('@')[0];
                response.Add(new JsonObject
                {
                    ["id"] = accountId,
                    ["displayName"] = accountId,
                    ["externalAuths"] = new JsonObject()
                });
            }

            await ResponseUtil.Json(ctx, response);
        });

        app.MapGet("/account/api/public/account/{accountId}", async (HttpContext ctx) =>
        {
            if (!AppConfig.GetBool("Config", "bUseConfigDisplayName", false))
                MemoryCurrentAccountId = RequestUtil.Route(ctx, "accountId");

            if (MemoryCurrentAccountId.Contains('@'))
                MemoryCurrentAccountId = MemoryCurrentAccountId.Split('@')[0];

            var accountId = RequestUtil.Route(ctx, "accountId");
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["id"] = accountId,
                ["displayName"] = MemoryCurrentAccountId,
                ["name"] = "Lawin",
                ["email"] = MemoryCurrentAccountId + "@lawin.com",
                ["failedLoginAttempts"] = 0,
                ["lastLogin"] = Functions.IsoNow(),
                ["numberOfDisplayNameChanges"] = 0,
                ["ageGroup"] = "UNKNOWN",
                ["headless"] = false,
                ["country"] = "US",
                ["lastName"] = "Server",
                ["preferredLanguage"] = "en",
                ["canUpdateDisplayName"] = false,
                ["tfaEnabled"] = false,
                ["emailVerified"] = true,
                ["minorVerified"] = false,
                ["minorExpected"] = false,
                ["minorStatus"] = "NOT_MINOR",
                ["cabinedMode"] = false,
                ["hasHashedEmail"] = false
            });
        });

        app.MapGet("/sdk/v1/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, (JsonNode)DataStore.Response("sdkv1.json").DeepClone()));

        app.MapPost("/auth/v1/oauth/token", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["access_token"] = "lawinstokenlol",
                ["token_type"] = "bearer",
                ["expires_in"] = 28800,
                ["expires_at"] = "9999-12-31T23:59:59.999Z",
                ["nonce"] = "lawinserver",
                ["features"] = new JsonArray
                {
                    "AntiCheat", "CommerceService", "Connect", "ContentService", "Ecom", "EpicConnect",
                    "Inventories", "LockerService", "MagpieService", "Matchmaking Service", "PCBService",
                    "QuestService", "Stats"
                },
                ["deployment_id"] = "lawinsdeploymentidlol",
                ["organization_id"] = "lawinsorganizationidlol",
                ["organization_user_id"] = "lawinsorganisationuseridlol",
                ["product_id"] = "prod-fn",
                ["product_user_id"] = "lawinsproductuseridlol",
                ["product_user_id_created"] = false,
                ["id_token"] = "lawinsidtokenlol",
                ["sandbox_id"] = "fn"
            }));

        app.MapGet("/epic/id/v2/sdk/accounts", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray
            {
                new JsonObject
                {
                    ["accountId"] = MemoryCurrentAccountId,
                    ["displayName"] = MemoryCurrentAccountId,
                    ["preferredLanguage"] = "en",
                    ["cabinedMode"] = false,
                    ["empty"] = false
                }
            }));

        app.MapPost("/epic/oauth/v2/token", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["scope"] = "basic_profile friends_list openid presence",
                ["token_type"] = "bearer",
                ["access_token"] = "lawinstokenlol",
                ["expires_in"] = 28800,
                ["expires_at"] = "9999-12-31T23:59:59.999Z",
                ["refresh_token"] = "lawinstokenlol",
                ["refresh_expires_in"] = 86400,
                ["refresh_expires_at"] = "9999-12-31T23:59:59.999Z",
                ["account_id"] = MemoryCurrentAccountId,
                ["client_id"] = "lawinsclientidlol",
                ["application_id"] = "lawinsapplicationidlol",
                ["selected_account_id"] = MemoryCurrentAccountId,
                ["id_token"] = "lawinstokenlol"
            }));

        app.MapPost("/epic/oauth/v2/tokenInfo", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["active"] = true,
                ["scope"] = "basic_profile friends_list openid offline_access presence",
                ["token_type"] = "bearer",
                ["expires_in"] = 28800,
                ["expires_at"] = "9999-12-31T23:59:59.999Z",
                ["account_id"] = MemoryCurrentAccountId,
                ["client_id"] = "lawinsclientidlol",
                ["application_id"] = "lawinsapplicationidlol"
            }));

        app.MapGet("/account/api/public/account/{accountId}/externalAuths", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray()));

        app.MapDelete("/account/api/oauth/sessions/kill", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));
        app.MapDelete("/account/api/oauth/sessions/kill/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Empty(ctx, 204));

        app.MapGet("/account/api/oauth/verify", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["token"] = "lawinstokenlol",
                ["session_id"] = "3c3662bcb661d6de679c636744c66b62",
                ["token_type"] = "bearer",
                ["client_id"] = "lawinsclientidlol",
                ["internal_client"] = true,
                ["client_service"] = "fortnite",
                ["account_id"] = MemoryCurrentAccountId,
                ["expires_in"] = 28800,
                ["expires_at"] = "9999-12-02T01:12:01.100Z",
                ["auth_method"] = "exchange_code",
                ["display_name"] = MemoryCurrentAccountId,
                ["app"] = "fortnite",
                ["in_app_id"] = MemoryCurrentAccountId,
                ["device_id"] = "lawinsdeviceidlol"
            }));

        app.MapPost("/account/api/oauth/token", async (HttpContext ctx) =>
        {
            var body = await RequestUtil.ReadObjectAsync(ctx);
            if (!AppConfig.GetBool("Config", "bUseConfigDisplayName", false))
                MemoryCurrentAccountId = body.GetStr("username") ?? "LawinServer";

            if (MemoryCurrentAccountId.Contains('@'))
                MemoryCurrentAccountId = MemoryCurrentAccountId.Split('@')[0];

            await ResponseUtil.Json(ctx, new JsonObject
            {
                ["access_token"] = "lawinstokenlol",
                ["expires_in"] = 28800,
                ["expires_at"] = "9999-12-02T01:12:01.100Z",
                ["token_type"] = "bearer",
                ["refresh_token"] = "lawinstokenlol",
                ["refresh_expires"] = 86400,
                ["refresh_expires_at"] = "9999-12-02T01:12:01.100Z",
                ["account_id"] = MemoryCurrentAccountId,
                ["client_id"] = "lawinsclientidlol",
                ["internal_client"] = true,
                ["client_service"] = "fortnite",
                ["displayName"] = MemoryCurrentAccountId,
                ["app"] = "fortnite",
                ["in_app_id"] = MemoryCurrentAccountId,
                ["device_id"] = "lawinsdeviceidlol"
            });
        });

        app.MapPost("/account/api/oauth/exchange", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonObject()));

        app.MapGet("/account/api/epicdomains/ssodomains", async (HttpContext ctx) =>
            await ResponseUtil.Json(ctx, new JsonArray
            {
                "unrealengine.com", "unrealtournament.com", "fortnite.com", "epicgames.com"
            }));

        app.MapPost("/fortnite/api/game/v2/tryPlayOnPlatform/account/{**rest}", async (HttpContext ctx) =>
            await ResponseUtil.Text(ctx, "true", "text/plain"));
    }
}
