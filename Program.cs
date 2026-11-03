using Microsoft.Extensions.FileProviders;
using LawinServer.Core;
using LawinServer.Routes;
using LawinServer.WebSockets;

var builder = WebApplication.CreateBuilder(args);

Paths.Root = builder.Environment.ContentRootPath;
AppConfig.Load();
ProfileStore.LoadAll();

var httpPort = int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var envPort)
    ? envPort
    : AppConfig.GetInt("Server", "httpPort", 3551);

builder.WebHost.UseUrls($"http://0.0.0.0:{httpPort}");

var app = builder.Build();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(Paths.PublicDir),
    ServeUnknownFileTypes = true
});

MainRoutes.Map(app);
PartyRoutes.Map(app);
DiscoveryRoutes.Map(app);
PrivacyRoutes.Map(app);
UserRoutes.Map(app);
ContentPagesRoutes.Map(app);
FriendsRoutes.Map(app);
StorefrontRoutes.Map(app);
VersionRoutes.Map(app);
LightswitchRoutes.Map(app);
AffiliateRoutes.Map(app);
MatchmakingRoutes.Map(app);
CloudStorageRoutes.Map(app);
McpRoutes.Map(app);
LockerApiRoutes.Map(app);
TimelineRoutes.Map(app);

app.MapFallback(ResponseUtil.NotFoundEpic);

_ = StartXmppHostAsync();

Console.WriteLine($"LawinServer started listening on port {httpPort}");
app.Run();

static async Task StartXmppHostAsync()
{
    try
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = Paths.Root
        });
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls($"http://0.0.0.0:{XmppServer.Port}");

        var xmpp = builder.Build();
        xmpp.UseWebSockets();
        xmpp.Run(async ctx =>
        {
            if (ctx.WebSockets.IsWebSocketRequest)
            {
                await XmppServer.HandleAsync(ctx);
            }
            else
            {
                ctx.Response.StatusCode = 404;
            }
        });

        await xmpp.StartAsync();
        Console.WriteLine($"XMPP and Matchmaker started listening on port {XmppServer.Port}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"XMPP and Matchmaker FAILED to start hosting: {ex.Message}");
    }
}
