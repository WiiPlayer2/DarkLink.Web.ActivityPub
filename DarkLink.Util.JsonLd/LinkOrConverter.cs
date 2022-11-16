using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DarkLink.Util.JsonLd;

internal class LinkOrConverter : JsonConverterFactory
{
    public static LinkOrConverter Instance = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkOr<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (JsonConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : JsonConverter<LinkOr<T>>
    {
        public override LinkOr<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var node = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
            if (node is null)
                return null;
            return TryGetLink(node)
                   ?? throw new NotImplementedException();
        }

        private LinkOr<T>? TryGetLink(JsonNode node)
        {
            var linkDto = node.Deserialize<LinkDto>();
            if (linkDto is null)
                return null;
            return new Link<T>(linkDto.Id);
        }

        public override void Write(Utf8JsonWriter writer, LinkOr<T> value, JsonSerializerOptions options)
        {
            if (value is Link<T> link)
                JsonSerializer.Serialize(writer, new LinkDto(link.Uri), options);
            else
                throw new NotImplementedException();
        }

        private record LinkDto([property: JsonPropertyName("@id")] Uri Id);
    }
}
