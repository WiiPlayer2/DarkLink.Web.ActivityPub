using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Serialization;
using DarkLink.Web.ActivityPub.Server;
using DarkLink.Web.ActivityPub.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
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
using Constants = DarkLink.Web.ActivityPub.Types.Constants;
using Object = DarkLink.Web.ActivityPub.Types.Object;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });
builder.Services.AddOptions<Config>().BindConfiguration(Config.KEY);
builder.Services.AddSingleton<ScopeDataSource>();
builder.Services.AddSingleton<ApplicationDataSource>();
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

await InitDataAsync();

app.MapGet("/profile", async ctx =>
{
    await DumpRequestAsync("Profile", ctx.Request);

    var data = await ReadData<PersonData>(GetProfilePath("data.json")) ?? throw new InvalidOperationException();

    var icon = default(LinkableList<Image>);
    if (File.Exists(GetProfilePath("icon.png")))
        icon = new Image
        {
            MediaType = "image/png",
            Url = ctx.BuildUri("profile/icon.png"),
        };

    var image = default(LinkableList<Image>);
    if (File.Exists(GetProfilePath("image.png")))
        image = new Image
        {
            MediaType = "image/png",
            Url = ctx.BuildUri("profile/image.png"),
        };

    var person = new Person(
        ctx.BuildUri("/inbox"),
        ctx.BuildUri("/outbox"))
    {
        Id = ctx.BuildUri("/profile"),
        PreferredUsername = config.Username,
        Name = data.Name,
        Summary = data.Summary,
        Icon = icon,
        Image = image,
        Followers = ctx.BuildUri("/followers"),
    };

    await ctx.Response.WriteLinkedData(person, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/api/whoami", () => Results.Redirect("/profile"));

app.MapGet("/profile/icon.png", ctx => ctx.Response.SendFileAsync(GetProfilePath("icon.png"), ctx.RequestAborted));

app.MapGet("/profile/image.png", ctx => ctx.Response.SendFileAsync(GetProfilePath("image.png"), ctx.RequestAborted));

app.MapGet("/notes/{id:guid}", async ctx =>
{
    var id = Guid.Parse((string) ctx.GetRouteValue("id")!);
    var note = await ReadNoteAsync(ctx, id, ctx.RequestAborted);
    if (note is null)
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
        await ctx.Response.CompleteAsync();
        return;
    }

    await ctx.Response.WriteLinkedData(note, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/notes/{id:guid}/activity", async ctx =>
{
    var id = Guid.Parse((string) ctx.GetRouteValue("id")!);
    var create = await ReadNoteCreateAsync(ctx, id, ctx.RequestAborted);
    if (create is null)
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
        await ctx.Response.CompleteAsync();
        return;
    }

    await ctx.Response.WriteLinkedData(create, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/outbox", async ctx =>
{
    var directoryInfo = new DirectoryInfo(GetNotePath(string.Empty));

    var creates = await directoryInfo
        .EnumerateFiles("*.json")
        .OrderBy(f => f.CreationTime)
        .Select(f => ReadNoteCreateAsync(ctx, Guid.Parse(Path.GetFileNameWithoutExtension(f.Name)), ctx.RequestAborted))
        .WhenAll(ctx.RequestAborted);

    var outboxCollection = new OrderedCollection
    {
        TotalItems = creates.Length,
        OrderedItems = DataList.FromItems(creates.Select(a => (LinkTo<Object>) a!)),
    };

    await ctx.Response.WriteLinkedData(outboxCollection, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/followers", async ctx =>
{
    var followerUris = await ReadData<IReadOnlyList<Uri>>(GetProfilePath("followers.json"), ctx.RequestAborted) ?? throw new InvalidOperationException();

    var followerCollection = new Collection
    {
        TotalItems = followerUris.Count,
        Items = DataList.FromItems(followerUris.Select(u => (LinkTo<Object>) u)),
    };

    await ctx.Response.WriteLinkedData(followerCollection, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapPost("/inbox", async ctx =>
{
    var activity = await ctx.Request.ReadLinkedData<Activity>(linkedDataOptions, ctx.RequestAborted) ?? throw new InvalidOperationException();

    switch (activity)
    {
        case Follow follow:
            await Follow(follow, ctx.RequestAborted);
            break;

        case Undo undo:
            await Undo(ctx, undo, ctx.RequestAborted);
            break;

        default:
            logger.LogWarning($"Activities of type {activity.GetType()} are not supported.");
            ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError; // TODO another error is definitely better
            break;
    }
});

app.MapPost("/outbox", async ctx => { throw new NotImplementedException(); })
    .RequireAuthorization();

//app.MapMethods(
//    "/{*path}",
//    new[] { HttpMethods.Get, HttpMethods.Post, },
//    async ctx =>
//    {
//        await DumpRequestAsync("<none>", ctx.Request, true);
//        ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
//        await ctx.Response.CompleteAsync();
//    });

app.MapRazorPages();
app.MapControllers();
app.Run();

async Task Follow(Follow follow, CancellationToken cancellationToken = default)
{
    var followersDataPath = GetProfilePath("followers.json");
    var followerUris = await ReadData<ISet<Uri>>(followersDataPath, cancellationToken) ?? throw new InvalidOperationException();

    followerUris.Add(follow.Actor.Value!.Match(link => link.Id, actor => actor.Id)!);

    await WriteData(followersDataPath, followerUris, cancellationToken);
}

async Task Unfollow(Follow follow, CancellationToken cancellationToken = default)
{
    var followersDataPath = GetProfilePath("followers.json");
    var followerUris = await ReadData<ISet<Uri>>(followersDataPath, cancellationToken) ?? throw new InvalidOperationException();

    followerUris.Remove(follow.Actor.Value!.Match(link => link.Id, actor => actor.Id)!);

    await WriteData(followersDataPath, followerUris, cancellationToken);
}

async Task Undo(HttpContext ctx, Undo undo, CancellationToken cancellationToken = default)
{
    var activity = undo.Object.Value!.Match(_ => throw new InvalidOperationException(), o => o);
    switch (activity)
    {
        case Follow follow:
            await Unfollow(follow, cancellationToken);
            break;

        default:
            logger.LogWarning($"Activities of type {activity.GetType()} are not supported while undoing.");
            ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError; // TODO another error is definitely better
            break;
    }
}

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

async Task<Note?> ReadNoteAsync(HttpContext ctx, Guid id, CancellationToken cancellationToken = default)
{
    var notePath = GetNotePath($"{id}.json");
    var data = await ReadData<NoteData>(notePath, cancellationToken);
    if (data is null)
        return null;

    var fileInfo = new FileInfo(notePath);
    var note = new Note
    {
        Id = ctx.BuildUri($"/notes/{id}"),
        Content = data.Content,
        To = DataList.FromItems(new LinkTo<Object>[]
        {
            Constants.Public,
            ctx.BuildUri("/followers"),
        }),
        //Published = data.Published ?? fileInfo.CreationTime,
    };
    return note;
}

async Task<Create?> ReadNoteCreateAsync(HttpContext ctx, Guid id, CancellationToken cancellationToken = default)
{
    var note = await ReadNoteAsync(ctx, id, cancellationToken);
    if (note is null)
        return null;

    var create = new Create
    {
        Id = ctx.BuildUri($"/notes/{id}/activity"),
        Actor = ctx.BuildUri("/profile"),
        Object = note,
        To = note.To,
        //Published = note.Published,
    };
    return create;
}

async Task InitDataAsync()
{
    await using var scopedServices = app.Services.CreateAsyncScope();
    var applicationManager = scopedServices.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
    var scopeManager = scopedServices.ServiceProvider.GetRequiredService<IOpenIddictScopeStoreResolver>().Get<OpenIddictEntityFrameworkCoreScope>();
    var userManager = scopedServices.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>();

    Directory.CreateDirectory(GetDataPath(string.Empty));
    Directory.CreateDirectory(GetProfilePath(string.Empty));
    Directory.CreateDirectory(GetNotePath(string.Empty));

    var profileDataPath = GetProfilePath("data.json");
    if (!File.Exists(profileDataPath))
        await File.WriteAllTextAsync(
            profileDataPath,
            JsonSerializer.Serialize(new PersonData("<no name>", default)));

    var followersDataPath = GetProfilePath("followers.json");
    if (!File.Exists(followersDataPath))
        await File.WriteAllTextAsync(followersDataPath, "[]");

    var initNoteDataPath = GetNotePath($"{Guid.Empty}.json");
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

string GetDataPath(string path)
    => Path.Combine(config!.DataDirectory, path);

string GetProfilePath(string path)
    => GetDataPath(Path.Combine("profile", path));

string GetNotePath(string path)
    => GetDataPath(Path.Combine("notes", path));

async Task<T?> ReadData<T>(string path, CancellationToken cancellationToken = default)
{
    if (!File.Exists(path))
        return default;

    await using var stream = File.OpenRead(path);
    var value = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
    return value;
}

async Task WriteData<T>(string path, T value, CancellationToken cancellationToken = default)
{
    await using var stream = File.Create(path);
    await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken);
}

internal record PersonData(string Name, string? Summary);

internal record NoteData(string Content, DateTimeOffset? Published);

internal record DeviceRegistrationRequest(
    [property: JsonPropertyName("client_name")]
    string ClientName,
    [property: JsonPropertyName("redirect_uris")]
    string RedirectUris,
    [property: JsonPropertyName("scopes")] string Scopes,
    [property: JsonPropertyName("website")]
    Uri Website);
