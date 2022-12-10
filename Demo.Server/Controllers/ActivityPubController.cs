using System.Net;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Server;
using DarkLink.Web.ActivityPub.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FileIO = System.IO.File;
using static DarkLink.Web.ActivityPub.Server.ActivityPubResults;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace Demo.Server.Controllers;

public class ActivityPubController : Controller
{
    private readonly APCore apCore;

    private readonly Config config;

    private readonly LinkedDataSerializationOptions linkedDataSerializationOptions;

    private readonly ILogger<ActivityPubController> logger;

    public ActivityPubController(APCore apCore, IOptions<Config> config, LinkedDataSerializationOptions linkedDataSerializationOptions, ILogger<ActivityPubController> logger)
    {
        this.apCore = apCore;
        this.linkedDataSerializationOptions = linkedDataSerializationOptions;
        this.logger = logger;
        this.config = config.Value;
    }

    [HttpGet, Route("~/followers"),]
    public async Task<IActionResult> GetFollowers()
    {
        var followerUris = await apCore.ReadData<IReadOnlyList<Uri>>(apCore.GetProfilePath("followers.json"), HttpContext.RequestAborted) ?? throw new InvalidOperationException();

        var followerCollection = new Collection
        {
            TotalItems = followerUris.Count,
            Items = DataList.FromItems(followerUris.Select(u => (LinkTo<Object>) u)),
        };

        return LinkedData(followerCollection, Constants.Context, linkedDataSerializationOptions);
    }

    [HttpGet, Route("~/note/{id:guid}"),]
    public async Task<IActionResult> GetNote(Guid id)
    {
        var note = await ReadNoteAsync(HttpContext, id, HttpContext.RequestAborted);
        if (note is null) return NotFound();

        return LinkedData(note, Constants.Context, linkedDataSerializationOptions);
    }

    [HttpGet, Route("~/note/{id:guid}/activity"),]
    public async Task<IActionResult> GetNoteActivity(Guid id)
    {
        var create = await ReadNoteCreateAsync(HttpContext, id, HttpContext.RequestAborted);
        if (create is null) return NotFound();

        return LinkedData(create, Constants.Context, linkedDataSerializationOptions);
    }

    [HttpGet, Route("~/outbox"),]
    public async Task<IActionResult> GetOutbox()
    {
        var directoryInfo = new DirectoryInfo(apCore.GetNotePath(string.Empty));

        var creates = await directoryInfo
            .EnumerateFiles("*.json")
            .OrderBy(f => f.CreationTime)
            .Select(f => ReadNoteCreateAsync(HttpContext, Guid.Parse(Path.GetFileNameWithoutExtension(f.Name)), HttpContext.RequestAborted))
            .WhenAll(HttpContext.RequestAborted);

        var outboxCollection = new OrderedCollection
        {
            TotalItems = creates.Length,
            OrderedItems = DataList.FromItems(creates.Select(a => (LinkTo<Object>) a!)),
        };

        return LinkedData(outboxCollection, Constants.Context, linkedDataSerializationOptions);
    }

    [HttpGet, Route("~/profile"),]
    public async Task<IActionResult> GetProfile()
    {
        //await DumpRequestAsync("Profile", ctx.Request);

        var data = await apCore.ReadData<PersonData>(apCore.GetProfilePath("data.json")) ?? throw new InvalidOperationException();

        var icon = default(LinkableList<Image>);
        if (FileIO.Exists(apCore.GetProfilePath("icon.png")))
            icon = new Image
            {
                MediaType = "image/png",
                Url = HttpContext.BuildUri("profile/icon.png"),
            };

        var image = default(LinkableList<Image>);
        if (FileIO.Exists(apCore.GetProfilePath("image.png")))
            image = new Image
            {
                MediaType = "image/png",
                Url = HttpContext.BuildUri("profile/image.png"),
            };

        var person = new Person(
            HttpContext.BuildUri("/inbox"),
            HttpContext.BuildUri("/outbox"))
        {
            Id = HttpContext.BuildUri("/profile"),
            PreferredUsername = config.Username,
            Name = data.Name,
            Summary = data.Summary,
            Icon = icon,
            Image = image,
            Followers = HttpContext.BuildUri("/followers"),
        };

        return LinkedData(person, Constants.Context, linkedDataSerializationOptions);
    }

    [HttpPost, Route("~/inbox"),]
    public async Task PostInbox()
    {
        var activity = await HttpContext.Request.ReadLinkedData<Activity>(linkedDataSerializationOptions, HttpContext.RequestAborted) ?? throw new InvalidOperationException();

        switch (activity)
        {
            case Follow follow:
                await apCore.Follow(follow, HttpContext.RequestAborted);
                break;

            case Undo undo:
                await apCore.Undo(HttpContext, undo, HttpContext.RequestAborted);
                break;

            default:
                logger.LogWarning($"Activities of type {activity.GetType()} are not supported.");
                HttpContext.Response.StatusCode = (int) HttpStatusCode.InternalServerError; // TODO another error is definitely better
                break;
        }
    }

    [HttpPost, Authorize, Route("~/outbox"),]
    public Task PostOutbox() => throw new NotImplementedException();

    private async Task<Note?> ReadNoteAsync(HttpContext ctx, Guid id, CancellationToken cancellationToken = default)
    {
        var notePath = apCore.GetNotePath($"{id}.json");
        var data = await apCore.ReadData<NoteData>(notePath, cancellationToken);
        if (data is null)
            return null;

        var note = new Note
        {
            Id = ctx.BuildUri($"/notes/{id}"),
            Content = data.Content,
            To = DataList.FromItems(new LinkTo<Object>[]
            {
                Constants.Public,
                ctx.BuildUri("/followers"),
            }),
        };
        return note;
    }

    private async Task<Create?> ReadNoteCreateAsync(HttpContext ctx, Guid id, CancellationToken cancellationToken = default)
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
        };
        return create;
    }
}
