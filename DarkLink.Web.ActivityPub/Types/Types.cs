using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Web.ActivityPub.Types.Extended;

namespace DarkLink.Web.ActivityPub.Types;

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
    public LinkableList<Object> Attachment { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}content")]
    public string? Content { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}icon")]
    public LinkableList<Image> Icon { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}image")]
    public LinkableList<Image> Image { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}name")]
    public string? Name { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}published")]
    public DateTimeOffset? Published { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}summary")]
    public string? Summary { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}to")]
    public LinkableList<Object> To { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}url")]
    public LinkableList<Object> Url { get; init; }
}

public abstract record BaseCollectionPage<TPage> : Object
    where TPage : BaseCollectionPage<TPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}next")]
    public LinkTo<TPage>? Next { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}partOf")]
    public LinkTo<Collection>? PartOf { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}prev")]
    public LinkTo<TPage>? Prev { get; init; }
}

public abstract record BaseCollection<TPage> : Object
    where TPage : BaseCollectionPage<TPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}current")]
    public LinkTo<TPage>? Current { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}first")]
    public LinkTo<TPage>? First { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}last")]
    public LinkTo<TPage>? Last { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}totalItems")]
    public int TotalItems { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}CollectionPage")]
public record CollectionPage : BaseCollectionPage<CollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public LinkableList<Object> Items { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Collection")]
public record Collection : BaseCollection<CollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public LinkableList<Object> Items { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}OrderedCollectionPage")]
public record OrderedCollectionPage : BaseCollectionPage<OrderedCollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public LinkableList<Object> OrderedItems { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}startIndex")]
    public int StartIndex { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}OrderedCollection")]
public record OrderedCollection : BaseCollection<OrderedCollectionPage>
{
    [LinkedDataProperty($"{Constants.NAMESPACE}items")]
    public LinkableList<Object> OrderedItems { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Activity")]
public record Activity : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}actor")]
    public LinkableList<Actor> Actor { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}object")]
    public LinkableList<Object> Object { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}IntransitiveActivity")]
public record IntransitiveActivity : Activity;
