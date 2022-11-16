using System.Text.Json;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

internal class DataListConverter : JsonConverterFactory
{
    private DataListConverter() { }

    public static DataListConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(DataList<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (JsonConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : JsonConverter<DataList<T>>
    {
        public override DataList<T>? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.TokenType == JsonTokenType.StartArray
                ? ReadEnumerable(ref reader, options)
                : ReadSingle(ref reader, options);

        private DataList<T> ReadEnumerable(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var list = new List<T>();
            reader.Read();
            while (reader.TokenType != JsonTokenType.EndArray) throw new NotImplementedException();

            return DataList.FromItems(list);
        }

        private DataList<T> ReadSingle(ref Utf8JsonReader reader, JsonSerializerOptions options)
        {
            var value = JsonSerializer.Deserialize<T>(ref reader, options);
            return DataList.From(value);
        }

        public override void Write(Utf8JsonWriter writer, DataList<T> value, JsonSerializerOptions options)
        {
            if (value.Count == 1)
                JsonSerializer.Serialize(writer, value.Value, options);
            else
                JsonSerializer.Serialize(writer, value.ToArray(), options);
        }
    }
}
