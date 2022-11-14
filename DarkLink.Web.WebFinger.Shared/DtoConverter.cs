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

    public override JsonResourceDescriptor? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => throw new NotImplementedException();

    public override void Write(Utf8JsonWriter writer, JsonResourceDescriptor value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, Map(value), options);
}
