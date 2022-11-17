using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Web.ActivityPub.Types.Extended;

[LinkedData(Constants.NAMESPACE)]
public record Person : Object;

[LinkedData(Constants.NAMESPACE)]
public record Document : Object;

[LinkedData(Constants.NAMESPACE)]
public record Image : Document;
