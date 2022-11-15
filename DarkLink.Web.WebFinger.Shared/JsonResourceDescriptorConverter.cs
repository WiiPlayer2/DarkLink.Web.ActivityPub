using System;
using System.Collections.Immutable;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkLink.Web.WebFinger.Shared;

public class JsonResourceDescriptorConverter : JsonConverter<JsonResourceDescriptor>
{
    private static JsonResourceDescriptorDto Map(JsonResourceDescriptor value)
        => new(
            value.Subject,
            value.Aliases.Any()
                ? value.Aliases.ToArray()
                : default,
            value.Properties.Any()
                ? value.Properties.ToDictionary(o => o.Key, o => o.Value)
                : default,
            value.Links.Any()
                ? value.Links.Select(Map).ToArray()
                : default);

    private static LinkDto Map(Link value)
        => new(
            value.Relation,
            value.Type,
            value.Href,
            value.Titles.Any()
                ? value.Titles.ToDictionary(o => o.Key, o => o.Value)
                : default,
            value.Properties.Any()
                ? value.Properties.ToDictionary(o => o.Key, o => o.Value)
                : default);

    private static JsonResourceDescriptor Map(JsonResourceDescriptorDto dto)
        => new(
            dto.Subject,
            dto.Aliases?.ToImmutableList() ?? ImmutableList<Uri>.Empty,
            dto.Properties?.ToImmutableDictionary() ?? ImmutableDictionary<Uri, string?>.Empty,
            dto.Links?.Select(Map).ToImmutableList() ?? ImmutableList<Link>.Empty);

    private static Link Map(LinkDto dto)
        => new(
            dto.Relation,
            dto.Type,
            dto.Href,
            dto.Titles?.ToImmutableDictionary() ?? ImmutableDictionary<string, string>.Empty,
            dto.Properties?.ToImmutableDictionary() ?? ImmutableDictionary<Uri, string?>.Empty);

    public override JsonResourceDescriptor? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var dto = JsonSerializer.Deserialize<JsonResourceDescriptorDto>(ref reader, options);
        return dto is null
            ? null
            : Map(dto);
    }

    public override void Write(Utf8JsonWriter writer, JsonResourceDescriptor value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, Map(value), options);
}
