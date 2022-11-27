using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Web.ActivityPub.Types.Extended;

public record TypedObject(DataList<Uri> Type) : Object;

[LinkedDataType($"{Constants.NAMESPACE}Relationship")]
public record Relationship : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}object")]
    public LinkTo<Object>? Object { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}relationship")]
    public Uri? Relation { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}subject")]
    public LinkTo<Object>? Subject { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Article")]
public record Article : Object;

[LinkedDataType($"{Constants.NAMESPACE}Document")]
public record Document : Object;

[LinkedDataType($"{Constants.NAMESPACE}Audio")]
public record Audio : Object;

[LinkedDataType($"{Constants.NAMESPACE}Image")]
public record Image : Document;

[LinkedDataType($"{Constants.NAMESPACE}Video")]
public record Video : Object;

[LinkedDataType($"{Constants.NAMESPACE}Note")]
public record Note : Object;

[LinkedDataType($"{Constants.NAMESPACE}Page")]
public record Page : Document;

[LinkedDataType($"{Constants.NAMESPACE}Event")]
public record Event : Object;

[LinkedDataType($"{Constants.NAMESPACE}Place")]
public record Place : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}accuracy")]
    public double? Accuracy { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}altitude")]
    public double? Altitude { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}latitude")]
    public double? Latitude { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}longitude")]
    public double? Longitude { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}radius")]
    public double? Radius { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}units")]
    public string? Units { get; init; } // TODO a special type might be better
}

[LinkedDataType($"{Constants.NAMESPACE}Mention")]
public record Mention : Link;

[LinkedDataType($"{Constants.NAMESPACE}Profile")]
public record Profile : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}describes")]
    public Object? Describes { get; init; }
}

[LinkedDataType($"{Constants.NAMESPACE}Tombstone")]
public record Tombstone : Object
{
    [LinkedDataProperty($"{Constants.NAMESPACE}deleted")]
    public DateTime? Deleted { get; init; }

    [LinkedDataProperty($"{Constants.NAMESPACE}formerType")]
    public DataList<Uri> FormerType { get; init; }
}
