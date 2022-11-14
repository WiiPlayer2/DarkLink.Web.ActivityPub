using System.Collections.Immutable;

namespace DarkLink.Web.WebFinger.Shared;

public record Link(
    string Relation,
    string? Type,
    Uri? Href,
    ImmutableDictionary<string, string> Titles,
    ImmutableDictionary<Uri, string?> Properties)
{
    public static Link Create(string relation)
        => new(relation,
            default,
            default,
            ImmutableDictionary<string, string>.Empty,
            ImmutableDictionary<Uri, string?>.Empty);
}
