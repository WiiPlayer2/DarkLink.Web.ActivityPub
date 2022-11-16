using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace DarkLink.Util.JsonLd;

internal class LinkedDataConverter : JsonConverterFactory
{
    private LinkedDataConverter() { }

    public static LinkedDataConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.GetCustomAttribute<LinkedDataAttribute>() is not null;

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options) => typeof(Conv<>).Create<JsonConverter>(typeToConvert);

    private class Conv<T> : JsonConverter<T>
    {
        public override T? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotImplementedException();

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            var metadata = typeof(T).GetCustomAttribute<LinkedDataAttribute>()!;
            var properties = typeof(T).GetProperties();
            var values = properties.Select(MapProperty);
            var node = new JsonObject(values);
            JsonSerializer.Serialize(writer, node, options);

            KeyValuePair<string, JsonNode?> MapProperty(PropertyInfo propertyInfo)
            {
                var name = ResolvePropertyName(propertyInfo);
                var propertyValue = propertyInfo.GetValue(value);
                var node = JsonSerializer.SerializeToNode(propertyValue, options);
                return new KeyValuePair<string, JsonNode?>(name, node);
            }

            string ResolvePropertyName(PropertyInfo propertyInfo)
            {
                var propertyNameAttribute = propertyInfo.GetCustomAttribute<JsonPropertyNameAttribute>();
                if (propertyNameAttribute is not null)
                    return propertyNameAttribute.Name;

                if (propertyInfo.Name.Equals("id", StringComparison.InvariantCultureIgnoreCase))
                    return "@id";

                if (propertyInfo.Name.Equals("type", StringComparison.InvariantCultureIgnoreCase))
                    return "@type";

                var name = $"{metadata.Path}{propertyInfo.Name.Uncapitalize()}";
                return name;
            }
        }
    }
}
