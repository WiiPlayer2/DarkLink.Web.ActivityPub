using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types.Extended;

namespace DarkLink.Web.ActivityPub.Types;

public static class Constants
{
    public const string NAMESPACE = "https://www.w3.org/ns/activitystreams#";

    public static readonly LinkedDataList<ContextEntry> Context = DataList.FromItems(new LinkOr<ContextEntry>[]
    {
        new Uri("https://www.w3.org/ns/activitystreams"),
        new ContextEntry
        {
            {new("inbox", UriKind.RelativeOrAbsolute), new Uri("ldp:inbox", UriKind.RelativeOrAbsolute)},
            {new("outbox", UriKind.RelativeOrAbsolute), new Uri("as:outbox", UriKind.RelativeOrAbsolute)},
            {new("url", UriKind.RelativeOrAbsolute), new Uri("as:url", UriKind.RelativeOrAbsolute)},
            {new("actor", UriKind.RelativeOrAbsolute), new Uri("as:actor", UriKind.RelativeOrAbsolute)},
            {new("published", UriKind.RelativeOrAbsolute), new Uri("as:published", UriKind.RelativeOrAbsolute)},
            {new("to", UriKind.RelativeOrAbsolute), new Uri("as:to", UriKind.RelativeOrAbsolute)},
            {new("attributedTo", UriKind.RelativeOrAbsolute), new Uri("as:attributedTo", UriKind.RelativeOrAbsolute)},
            {new("totalItems", UriKind.RelativeOrAbsolute), new Uri("as:totalItems", UriKind.RelativeOrAbsolute)},
        }!,
    });
}

internal class ActivityStreamsContextProxyResolver : IContextProxyResolver
{
    public IEnumerable<Type> ResolveProxyTypes(Type proxiedType) => typeof(Entity).Assembly.GetExportedTypes()
        .Where(t => (t.Namespace?.StartsWith(typeof(Entity).Namespace!) ?? false)
                    && !t.IsAbstract);
}

[ContextProxy(ProxyTypeResolver = typeof(ActivityStreamsContextProxyResolver))]
public abstract record Entity
{
    public DataList<LinkOr<Object>> AttributedTo { get; init; }

    [LinkedData("@id")] public Uri? Id { get; init; }

    public string? MediaType { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record Link : Entity { }

[LinkedData(Constants.NAMESPACE)]
public record Object : Entity
{
    public DataList<LinkOr<Object>> Attachment { get; init; }

    public string? Content { get; init; }

    public DataList<LinkOr<Image>> Icon { get; init; }

    public string? Name { get; init; }

    public DateTimeOffset? Published { get; init; }

    public string? Summary { get; init; }

    public DataList<LinkOr<Object>> To { get; init; }

    public DataList<LinkOr<Link>> Url { get; init; }
}

public abstract record BaseCollectionPage<TPage>
    where TPage : BaseCollectionPage<TPage>
{
    public LinkOr<TPage>? Next { get; init; }

    public LinkOr<Collection>? PartOf { get; init; }

    public LinkOr<TPage>? Prev { get; init; }
}

public abstract record BaseCollection<TPage> : Object
    where TPage : BaseCollectionPage<TPage>
{
    public LinkOr<TPage>? Current { get; init; }

    public LinkOr<TPage>? First { get; init; }

    public LinkOr<TPage>? Last { get; init; }

    public int TotalItems { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record CollectionPage : BaseCollectionPage<CollectionPage>
{
    public DataList<LinkOr<Object>> Items { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record Collection : BaseCollection<CollectionPage>
{
    public DataList<LinkOr<Object>> Items { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record OrderedCollectionPage : BaseCollectionPage<OrderedCollectionPage>
{
    [JsonPropertyName($"{Constants.NAMESPACE}items")]
    public DataList<LinkOr<Object>> OrderedItems { get; init; }

    public int StartIndex { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record OrderedCollection : BaseCollection<OrderedCollectionPage>
{
    [JsonPropertyName($"{Constants.NAMESPACE}items")]
    public DataList<LinkOr<Object>> OrderedItems { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record Activity : Object
{
    public DataList<LinkTo<Actor>> Actor { get; init; }

    public DataList<LinkTo<Object>> Object { get; init; }
}
