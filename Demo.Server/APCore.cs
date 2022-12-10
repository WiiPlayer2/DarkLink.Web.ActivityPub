using System.Net;
using System.Text.Json;
using DarkLink.Web.ActivityPub.Types.Extended;
using Microsoft.Extensions.Options;

namespace Demo.Server;

public class APCore
{
    private readonly Config config;

    private readonly ILogger<APCore> logger;

    public APCore(IOptions<Config> config, ILogger<APCore> logger)
    {
        this.logger = logger;
        this.config = config.Value;
    }

    public async Task Follow(Follow follow, CancellationToken cancellationToken = default)
    {
        var followersDataPath = GetProfilePath("followers.json");
        var followerUris = await ReadData<ISet<Uri>>(followersDataPath, cancellationToken) ?? throw new InvalidOperationException();

        followerUris.Add(follow.Actor.Value!.Match(link => link.Id, actor => actor.Id)!);

        await WriteData(followersDataPath, followerUris, cancellationToken);
    }

    public string GetDataPath(string path)
        => Path.Combine(config!.DataDirectory, path);

    public string GetNotePath(string path)
        => GetDataPath(Path.Combine("notes", path));

    public string GetProfilePath(string path)
        => GetDataPath(Path.Combine("profile", path));

    public async Task<T?> ReadData<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            return default;

        await using var stream = File.OpenRead(path);
        var value = await JsonSerializer.DeserializeAsync<T>(stream, cancellationToken: cancellationToken);
        return value;
    }

    public async Task Undo(HttpContext ctx, Undo undo, CancellationToken cancellationToken = default)
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

    private async Task Unfollow(Follow follow, CancellationToken cancellationToken = default)
    {
        var followersDataPath = GetProfilePath("followers.json");
        var followerUris = await ReadData<ISet<Uri>>(followersDataPath, cancellationToken) ?? throw new InvalidOperationException();

        followerUris.Remove(follow.Actor.Value!.Match(link => link.Id, actor => actor.Id)!);

        await WriteData(followersDataPath, followerUris, cancellationToken);
    }

    private async Task WriteData<T>(string path, T value, CancellationToken cancellationToken = default)
    {
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, value, cancellationToken: cancellationToken);
    }
}
