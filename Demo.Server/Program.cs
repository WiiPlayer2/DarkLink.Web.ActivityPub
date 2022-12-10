using System.Text.Json;
using DarkLink.Util.JsonLd;
using DarkLink.Web.ActivityPub.Serialization;
using DarkLink.Web.WebFinger.Server;
using Demo.Server;
using MemoryStorage.DataSource;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using static OpenIddict.Server.OpenIddictServerEvents;
using ASLink = DarkLink.Web.ActivityPub.Types.Link;

var linkedDataOptions = new LinkedDataSerializationOptions
{
    Converters =
    {
        new LinkToConverter(),
        new LinkableListConverter(),
    },
    TypeResolvers =
    {
        new ActivityPubTypeResolver(),
    },
};

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();
builder.Services.AddSingleton<APCore>();
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });
builder.Services.AddOptions<Config>().BindConfiguration(Config.KEY);
builder.Services.AddSingleton<ScopeDataSource>();
builder.Services.AddSingleton<ApplicationDataSource>();
builder.Services.AddSingleton(linkedDataOptions);
//builder.Services.AddAuthentication(options =>
//{
//    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
//});
//builder.Services.AddAuthorization();

builder.Services.AddDefaultIdentity<IdentityUser>(options =>
    {
        options.Password.RequiredLength = 1;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredUniqueChars = 0;
    })
    .AddEntityFrameworkStores<OpenIdContext>();

builder.Services.AddDbContext<OpenIdContext>(options =>
{
    options.UseInMemoryDatabase("in-memory");
    options.UseOpenIddict();
});

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<OpenIdContext>();
    })
    .AddServer(options =>
    {
        // Enable the endpoints.
        options
            .SetAuthorizationEndpointUris("/oauth/authorize")
            .SetTokenEndpointUris("/oauth/token");

        // Enable the authorization code flow.
        options.AllowAuthorizationCodeFlow()
            .AllowRefreshTokenFlow();
        options.AllowPasswordFlow();

        // Accept anonymous clients (i.e clients that don't send a client_id).
        options.AcceptAnonymousClients();

        // Register the signing and encryption credentials.
        options.AddDevelopmentEncryptionCertificate()
            .AddDevelopmentSigningCertificate();

        // Register the ASP.NET Core host and configure the authorization endpoint
        // to allow the /authorize minimal API handler to handle authorization requests
        // after being validated by the built-in OpenIddict server event handlers.
        //
        // Token requests will be handled by OpenIddict itself by reusing the identity
        // created by the /authorize handler and stored in the authorization codes.
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough();

        options.AddEventHandler<HandleTokenRequestContext>(builder => builder.UseScopedHandler<TokenRequestHandler>());
        //options.AddEventHandler<ValidateAuthorizationRequestContext>(builder => builder.UseSingletonHandler<AuthorizationRequestValidator>().SetOrder(0));
        options.IgnoreEndpointPermissions();
        options.IgnoreResponseTypePermissions();
        options.IgnoreGrantTypePermissions();
        options.IgnoreScopePermissions();
    })
    // Register the OpenIddict validation components.
    .AddValidation(options =>
    {
        // Import the configuration from the local OpenIddict server instance.
        options.UseLocalServer();

        // Register the ASP.NET Core host.
        options.UseAspNetCore();
    });

var app = builder.Build();
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebFinger();

var logger = app.Services.GetRequiredService<ILogger<Program>>();
var config = app.Services.GetRequiredService<IOptions<Config>>().Value;
var apCore = app.Services.GetRequiredService<APCore>();

await InitDataAsync();

app.MapGet("/profile/icon.png", ctx => ctx.Response.SendFileAsync(apCore.GetProfilePath("icon.png"), ctx.RequestAborted));
app.MapGet("/profile/image.png", ctx => ctx.Response.SendFileAsync(apCore.GetProfilePath("image.png"), ctx.RequestAborted));
app.MapGet("/api/whoami", () => Results.Redirect("/profile"));

//app.MapMethods(
//    "/{*path}",
//    new[] {HttpMethods.Get, HttpMethods.Post,},
//    async ctx =>
//    {
//        await DumpRequestAsync("<none>", ctx.Request, true);
//        ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
//        await ctx.Response.CompleteAsync();
//    });

app.MapRazorPages();
app.MapControllers();
app.Run();

async Task DumpRequestAsync(string topic, HttpRequest request, bool dumpBody = false)
{
    var headers = string.Join('\n', request.Headers.Select(h => $"{h.Key}: {h.Value}"));
    var query = string.Join('\n', request.Query.Select(q => $"{q.Key}={q.Value}"));

    var body = "<no dump>";
    if (dumpBody)
    {
        using var reader = new StreamReader(request.Body);
        body = await reader.ReadToEndAsync();
    }

    logger.LogDebug($"{topic}\n{request.Method} {request.GetDisplayUrl()}\n{query}\n{headers}\n{body}");
}

async Task InitDataAsync()
{
    await using var scopedServices = app.Services.CreateAsyncScope();
    var applicationManager = scopedServices.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var scopeManager = scopedServices.ServiceProvider.GetRequiredService<IOpenIddictScopeStoreResolver>().Get<OpenIddictEntityFrameworkCoreScope>();
    var userManager = scopedServices.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    Directory.CreateDirectory(apCore.GetDataPath(string.Empty));
    Directory.CreateDirectory(apCore.GetProfilePath(string.Empty));
    Directory.CreateDirectory(apCore.GetNotePath(string.Empty));

    var profileDataPath = apCore.GetProfilePath("data.json");
    if (!File.Exists(profileDataPath))
        await File.WriteAllTextAsync(
            profileDataPath,
            JsonSerializer.Serialize(new PersonData("<no name>", default)));

    var followersDataPath = apCore.GetProfilePath("followers.json");
    if (!File.Exists(followersDataPath))
        await File.WriteAllTextAsync(followersDataPath, "[]");

    var initNoteDataPath = apCore.GetNotePath($"{Guid.Empty}.json");
    if (!File.Exists(initNoteDataPath))
        await File.WriteAllTextAsync(
            initNoteDataPath,
            JsonSerializer.Serialize(new NoteData("🚩 This marks the start. 🚩", DateTimeOffset.Now)));

    await applicationManager.CreateAsync(
        new OpenIddictApplicationDescriptor
        {
            ClientId = "no",
            ClientSecret = "no",
            RedirectUris =
            {
                new Uri("http://oauth-redirect.andstatus.org"),
            },
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new OpenIddictEntityFrameworkCoreScope
        {
            Name = "read",
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new OpenIddictEntityFrameworkCoreScope
        {
            Name = "write",
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new OpenIddictEntityFrameworkCoreScope
        {
            Name = "follow",
        },
        CancellationToken.None);

    await userManager.CreateAsync(new IdentityUser(config.Username), config.Password);
}
