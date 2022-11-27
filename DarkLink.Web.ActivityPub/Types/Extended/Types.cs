using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Web.ActivityPub.Types.Extended;

public abstract record Actor(
    [property: LinkedDataProperty("http://www.w3.org/ns/ldp#inbox")]
    Uri Inbox,
    [property: LinkedDataProperty($"{Constants.NAMESPACE}outbox")]
    Uri Outbox) : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}followers")]
    public Uri? Followers { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}following")]
    public Uri? Following { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}liked")]
    public Uri? Liked { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}preferredUsername")]
    public string? PreferredUsername { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Person")]
public record Person(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedDataType($"{Constants.NAMESPACE}Document")]
public record Document : Object;

[LinkedDataType($"{Constants.NAMESPACE}Image")]
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
