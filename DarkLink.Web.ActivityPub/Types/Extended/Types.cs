using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Web.ActivityPub.Types.Extended;

public abstract record Actor(
    [property: JsonPropertyName("http://www.w3.org/ns/ldp#inbox")]
    Uri Inbox,
    Uri Outbox) : Object
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
public record TypedActivity(
    [property: LinkedData("@type")] Uri Type) : Activity;

[LinkedData(Constants.NAMESPACE)]
public record TypedObject(
    [property: LinkedData("@type")] Uri Type) : Object;

[LinkedData(Constants.NAMESPACE)]
public record Create : Activity;

[LinkedData(Constants.NAMESPACE)]
public record Note : Object;
