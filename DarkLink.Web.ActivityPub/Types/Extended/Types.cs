using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Web.ActivityPub.Types.Extended;

public record Actor(Uri Inbox, Uri Outbox) : Object
{
    public Uri? Followers { get; init; }

    public Uri? Following { get; init; }

    public Uri? Liked { get; init; }

    public string? PreferredUsername { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record Person(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedData(Constants.NAMESPACE)]
public record Document : Object;

[LinkedData(Constants.NAMESPACE)]
public record Image : Document;

[LinkedData(Constants.NAMESPACE)]
public record TypedActivity(Uri Type) : Activity;

[LinkedData(Constants.NAMESPACE)]
public record TypedObject(Uri Type) : Object;
