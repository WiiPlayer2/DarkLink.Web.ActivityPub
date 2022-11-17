using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityVocabulary.Extended;

namespace DarkLink.Web.ActivityVocabulary;

public static class Constants
{
    public const string NAMESPACE = "https://www.w3.org/ns/activitystreams#";
}

public abstract record Entity
{
    public Uri? Id { get; init; }

    public string? MediaType { get; init; }
}

[LinkedData(Constants.NAMESPACE)]
public record Link : Entity { }

[LinkedData(Constants.NAMESPACE)]
public record Object : Entity
{
    public DataList<LinkOr<Object>> Attachment { get; init; }

    public DataList<LinkOr<Image>> Icon { get; init; }

    public string? Name { get; init; }

    public string? Summary { get; init; }

    public DataList<LinkOr<Link>> Url { get; init; }
}
