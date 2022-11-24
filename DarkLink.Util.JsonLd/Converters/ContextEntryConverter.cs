using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

internal class ContextEntryConverter : JsonConverter<ContextEntry>
{
    private ContextEntryConverter() { }

    public static ContextEntryConverter Instance { get; } = new();

    public override ContextEntry? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => JsonSerializer.Deserialize<Dictionary<string, TermMapping>>(ref reader, options)?
            .Aggregate(new ContextEntry(), (contextEntry, kv) =>
            {
                contextEntry.Add(new Uri(kv.Key, UriKind.RelativeOrAbsolute), kv.Value);
                return contextEntry;
            });

    public override void Write(Utf8JsonWriter writer, ContextEntry value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value.ToDictionary(o => o.Key.ToString(), o => o.Value), options);
}

internal class TermMappingConverter : JsonConverter<TermMapping>
{
    private TermMappingConverter() { }

    public static TermMappingConverter Instance { get; } = new();

    public override TermMapping? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var node = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
        if (node is JsonValue jsonValue && jsonValue.TryGetValue<Uri>(out var id))
            return id;

        var dto = node.Deserialize<Dto>(options);
        if (dto is null)
            return null;

        return new TermMapping(dto.Id, dto.Type);
    }

    public override void Write(Utf8JsonWriter writer, TermMapping value, JsonSerializerOptions options)
    {
        if (value == (TermMapping) value.Id)
        {
            JsonSerializer.Serialize(writer, value.Id, options);
            return;
        }

        var dto = new Dto(value.Id, value.Type);
        JsonSerializer.Serialize(writer, dto, options);
    }

    [LinkedData(IsTypeless = true)]
    private record Dto(Uri Id, Uri? Type);
}
