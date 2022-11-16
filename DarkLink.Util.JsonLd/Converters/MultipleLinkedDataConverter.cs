using System.Text.Json;
using System.Text.Json.Serialization;

namespace DarkLink.Util.JsonLd.Converters;

internal class MultipleLinkedDataConverter : JsonConverterFactory
{
    private MultipleLinkedDataConverter() { }

    public static MultipleLinkedDataConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(IReadOnlyList<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (JsonConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : JsonConverter<IReadOnlyList<T>>
    {
        public override IReadOnlyList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.StartArray
                ? ReadEnumerable(ref reader, options)
                : ReadSingle(ref reader, options);

        private IReadOnlyList<T> ReadEnumerable(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var list = new List<T>();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndArray) throw new NotImplementedException();

            return list;
        }

        private IReadOnlyList<T> ReadSingle(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return value is null ? Array.Empty<T>() : new[] {value,};
        }

        public override void Write(Utf8JsonWriter writer, IReadOnlyList<T> value, JsonSerializerOptions options)
        {
            if (value.Count == 1)
                JsonSerializer.Serialize(writer, value[0]);
            else
                JsonSerializer.Serialize(writer, value);
        }
    }
}
