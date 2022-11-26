using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Web.ActivityPub.Types;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace DarkLink.Web.ActivityPub.Serialization;

public class LinkToConverter : JsonConverterFactory
{
    private LinkToConverter() { }

    public static LinkToConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkTo<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (JsonConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : JsonConverter<LinkTo<T>>
        where T : Object
    {
        public override LinkTo<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            var node = JsonSerializer.Deserialize<JsonNode>(ref reader, options);
            if (node is null)
                return null;

            if (node is JsonValue nodeValue && nodeValue.TryGetValue<string>(out var id))
                return new Uri(id);

            return node.Deserialize<T>(options)!;
        }

        public override void Write(Utf8JsonWriter writer, LinkTo<T> value, JsonSerializerOptions options)
        {
            value.Match(link =>
                {
                    // TODO Differentiate between only link and link object
                    JsonSerializer.Serialize(writer, link.Id, options);
                    return true;
                },
                obj =>
                {
                    JsonSerializer.Serialize(writer, obj, options);
                    return true;
                });
        }
    }
}
