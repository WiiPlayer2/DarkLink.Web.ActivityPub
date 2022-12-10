using System.Collections.Immutable;
using System.Net;
using System.Net.Mime;
using System.Security.Claims;
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
using MemoryStorage.Domain;
using MemoryStorage.Stores;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;
using Application = MemoryStorage.Domain.Application;
using ASLink = DarkLink.Web.ActivityPub.Types.Link;
using Authorization = MemoryStorage.Domain.Authorization;
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

app.MapPost("/api/v1/apps", async ctx =>
{
    var request = await ctx.Request.ReadFromJsonAsync<DeviceRegistrationRequest>() ?? throw new InvalidOperationException();
    var response = new OpenIddictResponse(new Dictionary<string, StringValues>
    {
        {"client_id", "no"},
        {"client_secret", "no"},
        {"client_secret_expires_at", "2893276800"},
        {"redirect_uris", request.RedirectUris},
        {"client_name", request.ClientName},
        {"grant_types", new[]{OpenIddictConstants.GrantTypes.Password, OpenIddictConstants.GrantTypes.AuthorizationCode, OpenIddictConstants.GrantTypes.RefreshToken}},
    });
    ctx.Response.StatusCode = (int) HttpStatusCode.Created;
    await ctx.Response.WriteAsJsonAsync(response);
});

app.MapMethods("/oauth/authorize", new[] {"GET", "POST"}, async ctx =>
{
    var applicationManager = ctx.RequestServices.GetRequiredService<IOpenIddictApplicationManager>();
    var authorizationManager = ctx.RequestServices.GetRequiredService<IOpenIddictAuthorizationManager>();
    var scopeManager = ctx.RequestServices.GetRequiredService<IOpenIddictScopeManager>();
    var userManager = ctx.RequestServices.GetRequiredService<UserManager<IdentityUser>>();

    var request = ctx.GetOpenIddictServerRequest() ?? throw new InvalidOperationException();
    var result = await ctx.AuthenticateAsync();

    if (!result.Succeeded || request.HasPrompt(Prompts.Login))
    {
        if (request.HasPrompt(Prompts.None))
        {
            await ctx.ForbidAsync(
                OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme,
                new(new Dictionary<string, string?>()
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in.",
                }));
            return;
        }

        var prompt = string.Join(" ", request.GetPrompts().Remove(Prompts.Login));

        var parameters = ctx.Request.HasFormContentType ? ctx.Request.Form.Where(parameter => parameter.Key != Parameters.Prompt).ToList() : ctx.Request.Query.Where(parameter => parameter.Key != Parameters.Prompt).ToList();

        parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

        await ctx.ChallengeAsync(
            IdentityConstants.ApplicationScheme,
            new()
            {
                RedirectUri = ctx.Request.PathBase + ctx.Request.Path + QueryString.Create(parameters),
            });
        return;
    }


    var user = await userManager.GetUserAsync(result.Principal) ??
               throw new InvalidOperationException("The user details cannot be retrieved.");
    var application = await applicationManager.FindByClientIdAsync(request.ClientId ?? throw new InvalidOperationException(), ctx.RequestAborted) ??
                      throw new InvalidOperationException();

    var authorizations = await authorizationManager.FindAsync(
        subject: await userManager.GetUserIdAsync(user),
        client: await applicationManager.GetIdAsync(application) ?? string.Empty,
        status: Statuses.Valid,
        type: AuthorizationTypes.Permanent,
        scopes: request.GetScopes()).ToListAsync();

    if (ctx.Request.HasFormContentType)
    {
        var form = await ctx.Request.ReadFormAsync();

        if (!string.IsNullOrEmpty(form["submit.Accept"]))
        {
            var identity = new ClaimsPrincipal(new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role));

            // Add the claims that will be persisted in the tokens.
            identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
                .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
                .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
                /*.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray())*/;

            // Note: in this sample, the granted scopes match the requested scope
            // but you may want to allow the user to uncheck specific scopes.
            // For that, simply restrict the list of scopes before calling SetScopes.
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            // Automatically create a permanent authorization to avoid requiring explicit consent
            // for future authorization or token requests containing the same scopes.
            var authorization = authorizations.LastOrDefault();
            authorization ??= await authorizationManager.CreateAsync(
                identity,
                subject: await userManager.GetUserIdAsync(user),
                client: await applicationManager.GetIdAsync(application) ?? throw new InvalidOperationException(),
                type: AuthorizationTypes.Permanent,
                scopes: identity.GetScopes());

            identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
            identity.SetDestinations(identity.GetDestinations());

            await ctx.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, identity);
            return;
        }
    }

    var consentType = await applicationManager.GetConsentTypeAsync(application);
    switch (consentType)
    {
        case ConsentTypes.External when !authorizations.Any():
            await ctx.ForbidAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "The logged in user is not allowed to access this client application."
                }));
            return;

        case ConsentTypes.Implicit:
        case ConsentTypes.External when authorizations.Any():
        case ConsentTypes.Explicit when authorizations.Any() && !request.HasPrompt(Prompts.Consent):
            // Create the claims-based identity that will be used by OpenIddict to generate tokens.
            var identity = new ClaimsPrincipal(new ClaimsIdentity(
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role));

            // Add the claims that will be persisted in the tokens.
            identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
                    /*.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray())*/;

            // Note: in this sample, the granted scopes match the requested scope
            // but you may want to allow the user to uncheck specific scopes.
            // For that, simply restrict the list of scopes before calling SetScopes.
            identity.SetScopes(request.GetScopes());
            identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

            // Automatically create a permanent authorization to avoid requiring explicit consent
            // for future authorization or token requests containing the same scopes.
            var authorization = authorizations.LastOrDefault();
            authorization ??= await authorizationManager.CreateAsync(
                principal: identity,
                subject: await userManager.GetUserIdAsync(user),
                client: await applicationManager.GetIdAsync(application) ?? throw new InvalidOperationException(),
                type: AuthorizationTypes.Permanent,
                scopes: identity.GetScopes());

            identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
            identity.SetDestinations(identity.GetDestinations());

            await ctx.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme, identity);
            return;

        case ConsentTypes.Explicit when request.HasPrompt(Prompts.None):
        case ConsentTypes.Systematic when request.HasPrompt(Prompts.None):
            await ctx.ForbidAsync(
                OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties(new Dictionary<string, string?>
                {
                    [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                    [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                        "Interactive user consent is required.",
                }));
            return;

        default:
            await ctx.Response.WriteAsync(@$"
<p class=""lead text-left"">Do you want to grant <strong>{applicationManager.GetLocalizedDisplayNameAsync(application)}</strong> access to your data? (scopes requested: {request.Scope})</p>
<form action=""/oauth/authorize"" method=""post"">
{string.Join("\n", (ctx.Request.HasFormContentType ? (IEnumerable<KeyValuePair<string, StringValues>>)ctx.Request.Form : ctx.Request.Query).Select(p => $"<input type=\"hidden\" name=\"{p.Key}\" value=\"{p.Value}\" />"))}
<input class=""btn btn-lg btn-success"" name=""submit.Accept"" type=""submit"" value=""Yes"" />
<input class=""btn btn-lg btn-danger"" name=""submit.Deny"" type=""submit"" value=""No"" />
</form>
", CancellationToken.None);
            return;
    }
});

app.MapMethods(
    "/{*path}",
    new[] { HttpMethods.Get, HttpMethods.Post, },
    async ctx =>
    {
        await DumpRequestAsync("<none>", ctx.Request, true);
        ctx.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
        await ctx.Response.CompleteAsync();
    });

app.MapRazorPages();
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
        new()
        {
            ClientId = "no",
            ClientSecret = "no",
            RedirectUris =
            {
                new("http://oauth-redirect.andstatus.org"),
            },
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new()
        {
            Name = "read",
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new()
        {
            Name = "write",
        },
        CancellationToken.None);

    await scopeManager.CreateAsync(
        new()
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
    StringValues RedirectUris,
    [property: JsonPropertyName("scopes")]
    string Scopes,
    [property: JsonPropertyName("website")]
    Uri Website);