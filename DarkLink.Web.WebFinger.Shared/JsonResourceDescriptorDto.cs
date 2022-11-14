using System.Text.Json.Serialization;

namespace DarkLink.Web.WebFinger.Shared;

internal record JsonResourceDescriptorDto(
    [property: JsonPropertyName("subject")]
    Uri? Subject = default,
    [property: JsonPropertyName("aliases")]
    Uri[]? Aliases = default,
    [property: JsonPropertyName("properties")]
    Dictionary<Uri, string?>? Properties = default,
    [property: JsonPropertyName("links")] LinkDto[]? Links = default);
