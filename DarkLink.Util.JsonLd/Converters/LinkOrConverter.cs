using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

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
        private LinkOr<T> GetObject(JsonNode node, JsonSerializerOptions options)
            => node.Deserialize<T>(options)!;

        public override LinkOr<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var node = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
            if (node is null)
                return null;
            return TryGetLink(node, options) ?? GetObject(node, options);
        }

        private LinkOr<T>? TryGetLink(JsonNode node, JsonSerializerOptions options)
        {
            if (node is not JsonObject {Count: 1} jsonObj || !jsonObj.ContainsKey("@id"))
                return null;

            var linkDto = node.Deserialize<LinkDto>(options)!;
            return new Link<T>(linkDto.Id);
        }

        public override void Write(Utf8JsonWriter writer, LinkOr<T> value, JsonSerializerOptions options)
            => value.Match(
                uri =>
                {
                    JsonSerializer.Serialize(writer, uri, options);
                    return true;
                },
                obj =>
                {
                    JsonSerializer.Serialize(writer, obj, options);
                    return true;
                });

        private record LinkDto([property: JsonPropertyName("@id")] Uri Id);
    }
}
