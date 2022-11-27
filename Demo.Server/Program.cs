using System.Diagnostics.CodeAnalysis;
using System.Net;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Serialization;
using DarkLink.Web.ActivityPub.Server;
using DarkLink.Web.ActivityPub.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
using DarkLink.Web.WebFinger.Server;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.HttpOverrides;
using ASLink = DarkLink.Web.ActivityPub.Types.Link;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;
using Object = DarkLink.Web.ActivityPub.Types.Object;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();
builder.Services.Configure<ForwardedHeadersOptions>(options => { options.ForwardedHeaders = ForwardedHeaders.All; });

var app = builder.Build();
app.UseForwardedHeaders();
app.UseWebFinger();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

var linkedDataOptions = new LinkedDataSerializationOptions
{
    Converters =
    {
        new LinkToConverter(),
        new LinkableListConverter(),
    },
    JsonSerializerOptions =
    {
        Converters =
        {
            //LinkToConverter.Instance,
        },
    },
};

app.MapGet("/profiles/{username}", (string username) => $"Welcome to the profile of [{username}].");

app.MapGet("/profile.png", async ctx => { await ctx.Response.SendFileAsync("./profile.png", ctx.RequestAborted); });

app.MapGet("/profiles/{username}.json", async ctx =>
{
    await DumpRequestAsync("Profile", ctx.Request);

    if (!ctx.Request.RouteValues.TryGetValue("username", out var usernameRaw)
        || usernameRaw is not string username)
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
        return;
    }

    if (!Directory.Exists($"./data/{username}"))
    {
        ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
        return;
    }

    ctx.Response.Headers.CacheControl = "max-age=0, private, must-revalidate";
    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";

    var person = new Person(
        new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/profiles/{username}/inbox"),
        new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/profiles/{username}/outbox"))
    {
        Id = new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/profiles/{username}.json"),
        PreferredUsername = username,
        Name = $"Waldemar Tomme [{username}]",
        Summary = "Just testing around 🧪",
        Url = DataList.From<LinkTo<Object>>(new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/profiles/{username}")),
        Icon = DataList.From<LinkTo<Image>>(new Image
        {
            MediaType = "image/png",
            Url = DataList.From<LinkTo<Object>>(new Uri($"{ctx.Request.Scheme}://{ctx.Request.Host}/profile.png")),
        }),
    };

    var node = LinkedDataSerializer.Serialize(person, Constants.Context, linkedDataOptions);

    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);
});

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

    var node = LinkedDataSerializer.Serialize(outboxCollection, Constants.Context, linkedDataOptions);

    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";
    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);
});

app.MapPost("/profiles/{username}/inbox", async ctx =>
{
    await DumpRequestAsync("[POST] Inbox", ctx.Request);

    if (!CheckRequest(ctx, out var username)) return;

    var data = await ctx.Request.ReadLinkedData<TypedActivity>(linkedDataOptions);
});

app.MapGet("/notes/{username}/{note}", async ctx =>
{
    await DumpRequestAsync("Note", ctx.Request);

    if (!CheckRequest(ctx, out var username)
        || !ctx.Request.RouteValues.TryGetValue("note", out var noteFileRaw)
        || noteFileRaw is not string noteFile) return;

    var note = await GetNoteAsync(ctx.Request.Scheme, ctx.Request.Host.ToString(), username, noteFile, ctx.RequestAborted);
    var node = LinkedDataSerializer.Serialize(note, Constants.Context, linkedDataOptions);

    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";
    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);
});

app.MapGet("/notes/{username}/{note}/activity", async ctx =>
{
    await DumpRequestAsync("Activity", ctx.Request);

    if (!CheckRequest(ctx, out var username)
        || !ctx.Request.RouteValues.TryGetValue("note", out var noteFileRaw)
        || noteFileRaw is not string noteFile) return;

    var activity = await GetNoteActivityAsync(ctx.Request.Scheme, ctx.Request.Host.ToString(), username, noteFile, ctx.RequestAborted);
    var node = LinkedDataSerializer.Serialize(activity, Constants.Context, linkedDataOptions);

    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";
    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);
});

app.MapMethods(
    "/{*path}",
    new[] {HttpMethods.Get, HttpMethods.Post},
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

internal static class Helper
{
    public static Task<T[]> WhenAll<T>(this IEnumerable<Task<T>> tasks, CancellationToken cancellationToken = default)
        => Task.WhenAll(tasks);
}
