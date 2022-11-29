using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Text.Json;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Serialization;
using DarkLink.Web.ActivityPub.Server;
using DarkLink.Web.ActivityPub.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
using DarkLink.Web.WebFinger.Server;
using Demo.Server;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using ASLink = DarkLink.Web.ActivityPub.Types.Link;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;
using Object = DarkLink.Web.ActivityPub.Types.Object;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });
builder.Services.AddOptions<Config>().BindConfiguration(Config.KEY);

var app = builder.Build();
app.UseForwardedHeaders();
//app.UseAuthentication();
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

    var data = await ReadData<PersonData>("profile/data.json") ?? throw new InvalidOperationException();

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
    };

    await ctx.Response.WriteLinkedData(person, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/profile/icon.png", ctx => ctx.Response.SendFileAsync(GetProfilePath("icon.png"), ctx.RequestAborted));

app.MapGet("/profile/image.png", ctx => ctx.Response.SendFileAsync(GetProfilePath("image.png"), ctx.RequestAborted));

app.MapGet("/profiles/{username}/outbox", async ctx =>
{
    await DumpRequestAsync("Outbox", ctx.Request);

    if (!CheckRequest(ctx, out var username)) return;

    var activities = await new DirectoryInfo($"./data/{username}")
        .EnumerateFiles("*.txt")
        .OrderBy(f => f.CreationTime)
        .Select(f => GetNoteActivityAsync(ctx.Request.Scheme, ctx.Request.Host.ToString(), username, f.Name, ctx.RequestAborted))
        .WhenAll();

    var outboxCollection = new OrderedCollection
    {
        TotalItems = activities.Length,
        OrderedItems = DataList.FromItems(activities.Select(a => (LinkTo<Object>) a!)),
    };

    await ctx.Response.WriteLinkedData(outboxCollection, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapPost("/profiles/{username}/inbox", async ctx =>
{
    await DumpRequestAsync("[POST] Inbox", ctx.Request);

    if (!CheckRequest(ctx, out var username)) return;

    var data = await ctx.Request.ReadLinkedData<Activity>(linkedDataOptions);
});

app.MapGet("/notes/{username}/{note}", async ctx =>
{
    await DumpRequestAsync("Note", ctx.Request);

    if (!CheckRequest(ctx, out var username)
        || !ctx.Request.RouteValues.TryGetValue("note", out var noteFileRaw)
        || noteFileRaw is not string noteFile) return;

    var note = await GetNoteAsync(ctx.Request.Scheme, ctx.Request.Host.ToString(), username, noteFile, ctx.RequestAborted);

    await ctx.Response.WriteLinkedData(note, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapGet("/notes/{username}/{note}/activity", async ctx =>
{
    await DumpRequestAsync("Activity", ctx.Request);

    if (!CheckRequest(ctx, out var username)
        || !ctx.Request.RouteValues.TryGetValue("note", out var noteFileRaw)
        || noteFileRaw is not string noteFile) return;

    var activity = await GetNoteActivityAsync(ctx.Request.Scheme, ctx.Request.Host.ToString(), username, noteFile, ctx.RequestAborted);

    await ctx.Response.WriteLinkedData(activity, Constants.Context, linkedDataOptions, ctx.RequestAborted);
});

app.MapMethods(
    "/{*path}",
    new[] {HttpMethods.Get, HttpMethods.Post,},
    async ctx =>
    {
        await DumpRequestAsync("<none>", ctx.Request, true);
        ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
        await ctx.Response.CompleteAsync();
    });

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

bool CheckRequest(HttpContext ctx, [NotNullWhen(true)] out string? username)
{
    username = default;
    if (!ctx.Request.RouteValues.TryGetValue("username", out var usernameRaw)
        || usernameRaw is not string usernameLocal)
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        return false;
    }

    if (!Directory.Exists($"./data/{usernameLocal}"))
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
        return false;
    }

    username = usernameLocal;
    return true;
}

async Task<Note> GetNoteAsync(string scheme, string host, string username, string filename, CancellationToken cancellationToken = default)
{
    var fileInfo = new FileInfo($"./data/{username}/{filename}");
    return new Note
    {
        Id = new Uri($"{scheme}://{host}/notes/{username}/{fileInfo.Name}"),
        To = DataList.From<LinkTo<Object>>(new Uri("https://www.w3.org/ns/activitystreams#Public")),
        AttributedTo = DataList.From<LinkTo<Object>>(new Uri($"{scheme}://{host}/profiles/{username}.json")),
        //Published = fileInfo.CreationTime,
        Content = await File.ReadAllTextAsync(fileInfo.FullName, cancellationToken),
    };
}

async Task<Create> GetNoteActivityAsync(string scheme, string host, string username, string filename, CancellationToken cancellationToken = default)
{
    var note = await GetNoteAsync(scheme, host, username, filename, cancellationToken);
    return new Create
    {
        Id = new Uri($"{note.Id}/activity"),
        //Published = note.Published,
        To = note.To,
        Actor = DataList.From<LinkTo<Actor>>(new Uri($"{scheme}://{host}/profiles/{username}.json")),
        Object = DataList.From<LinkTo<Object>>(note),
    };
}

async Task InitDataAsync()
{
    Directory.CreateDirectory(GetDataPath(string.Empty));
    Directory.CreateDirectory(GetProfilePath(string.Empty));
    await File.WriteAllTextAsync(
        GetProfilePath("data.json"),
        JsonSerializer.Serialize(new PersonData("<no name>", default)));
}

string GetDataPath(string path)
    => Path.Combine(config!.DataDirectory, path);

string GetProfilePath(string path)
    => GetDataPath(Path.Combine("profile", path));

async Task<T?> ReadData<T>(string file, CancellationToken cancellationToken = default)
{
    var path = GetDataPath(file);
    if (!File.Exists(path))
        return default;

    await using var stream = File.OpenRead(path);
    var value = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
    return value;
}

internal static class Helper
{
    public static Uri BuildUri(this HttpContext ctx, string path)
        => new($"{ctx.Request.Scheme}://{ctx.Request.Host}/{path.TrimStart('/')}");

    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
        => Task.WhenAll(tasks);
}

internal record PersonData(string Name, string? Summary);
