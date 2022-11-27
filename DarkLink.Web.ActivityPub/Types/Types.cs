using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
using LDConstants = DarkLink.Util.JsonLd.Constants;

namespace DarkLink.Web.ActivityPub.Types;

public static class Constants
{
    public const string NAMESPACE = "https://www.w3.org/ns/activitystreams#";

    public static readonly LinkedDataList<ContextEntry> Context = DataList.FromItems(new LinkOr<ContextEntry>[]
    {
        new Uri("https://www.w3.org/ns/activitystreams"),
        new ContextEntry
        {
            MapId("inbox", "ldp:inbox"),
            MapId("outbox", "as:outbox"),
            MapId("url", "as:url"),
            MapId("actor", "as:actor"),
            Map("published", "as:published", "xsd:dateTime"),
            MapId("to", "as:to"),
            MapId("attributedTo", "as:attributedTo"),
            {new("totalItems", UriKind.RelativeOrAbsolute), new Uri("as:totalItems", UriKind.RelativeOrAbsolute)},
        }!,
    });

    private static (Uri Id, TermMapping Mapping) Map(string property, string iri, string type)
        => (new Uri(property, UriKind.Relative),
            new TermMapping(new Uri(iri, UriKind.RelativeOrAbsolute))
            {
                Type = new Uri(type, UriKind.RelativeOrAbsolute),
            });

    private static (Uri Id, TermMapping Mapping) MapId(string property, string iri)
        => (new Uri(property, UriKind.Relative),
            new TermMapping(new Uri(iri, UriKind.RelativeOrAbsolute))
            {
                Type = LDConstants.Id,
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
    [LinkedDataProperty($"{Constants.NAMESPACE}attributedTo")]
    public LinkableList<Object> AttributedTo { get; init; }

    public Uri? Id { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}mediaType")]
    public string? MediaType { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Link")]
public record Link : Entity { }

[LinkedDataType($"{Constants.NAMESPACE}Object")]
public record Object : Entity
{
    [LinkedDataProperty($"{Constants.NAMESPACE}attachment")]
    public DataList<LinkOr<Object>> Attachment { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}content")]
    public string? Content { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}icon")]
    public DataList<LinkTo<Image>> Icon { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}name")]
    public string? Name { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}published")]
    public DateTimeOffset? Published { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}summary")]
    public string? Summary { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}to")]
    public LinkableList<Object> To { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}url")]
    public DataList<LinkTo<Object>> Url { get; init; }
}

public abstract record BaseCollectionPage<TPage>
    where TPage : BaseCollectionPage<TPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}next")]
    public LinkOr<TPage>? Next { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}partOf")]
    public LinkOr<Collection>? PartOf { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}prev")]
    public LinkOr<TPage>? Prev { get; init; }
}

public abstract record BaseCollection<TPage> : Object
    where TPage : BaseCollectionPage<TPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}current")]
    public LinkOr<TPage>? Current { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}first")]
    public LinkOr<TPage>? First { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}last")]
    public LinkOr<TPage>? Last { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}totalItems")]
    public int TotalItems { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}CollectionPage")]
public record CollectionPage : BaseCollectionPage<CollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public DataList<LinkOr<Object>> Items { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Collection")]
public record Collection : BaseCollection<CollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public DataList<LinkOr<Object>> Items { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}OrderedCollectionPage")]
public record OrderedCollectionPage : BaseCollectionPage<OrderedCollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public DataList<LinkTo<Object>> OrderedItems { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}startIndex")]
    public int StartIndex { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}OrderedCollection")]
public record OrderedCollection : BaseCollection<OrderedCollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public DataList<LinkTo<Object>> OrderedItems { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Activity")]
public record Activity : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}actor")]
    public DataList<LinkTo<Actor>> Actor { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}object")]
    public DataList<LinkTo<Object>> Object { get; init; }
}
