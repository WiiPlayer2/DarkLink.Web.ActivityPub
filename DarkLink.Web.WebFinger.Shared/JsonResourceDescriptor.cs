using System.Collections.Immutable;

namespace DarkLink.Web.WebFinger.Shared;

public record JsonResourceDescriptor(
    Uri? Subject,
    ImmutableList<Uri> Aliases,
    ImmutableDictionary<Uri, string?> Properties,
    ImmutableList<Link> Links)
{
    public static JsonResourceDescriptor Empty { get; } = new(
        default,
        ImmutableList<Uri>.Empty,
        ImmutableDictionary<Uri, string?>.Empty,
        ImmutableList<Link>.Empty);
}
