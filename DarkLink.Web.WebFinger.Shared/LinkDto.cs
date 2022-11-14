using System.Text.Json.Serialization;

namespace DarkLink.Web.WebFinger.Shared;

internal record LinkDto(
    [property: JsonPropertyName("rel")] string Relation,
    [property: JsonPropertyName("type")] string? Type = default,
    [property: JsonPropertyName("href")] Uri? Href = default,
    [property: JsonPropertyName("titles")] Dictionary<string, string>? Titles = default,
    [property: JsonPropertyName("properties")]
    Dictionary<Uri, string?>? Properties = default);
