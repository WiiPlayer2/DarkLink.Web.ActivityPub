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

[LinkedDataType($"{Constants.NAMESPACE}Application")]
public record Application(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedDataType($"{Constants.NAMESPACE}Group")]
public record Group(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedDataType($"{Constants.NAMESPACE}Organization")]
public record Organization(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedDataType($"{Constants.NAMESPACE}Person")]
public record Person(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);

[LinkedDataType($"{Constants.NAMESPACE}Service")]
public record Service(Uri Inbox, Uri Outbox) : Actor(Inbox, Outbox);
