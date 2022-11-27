using System.Text.Json;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters.Json;

internal class LinkedDataListConverter : JsonConverterFactory
{
    private LinkedDataListConverter() { }

    public static LinkedDataListConverter Instance { get; } = new();

    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkedDataList<>);

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (JsonConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : JsonConverter<LinkedDataList<T>>
    {
        public override LinkedDataList<T> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => JsonSerializer.Deserialize<DataList<LinkOr<T>>>(ref reader, options);

        public override void Write(Utf8JsonWriter writer, LinkedDataList<T> value, JsonSerializerOptions options)
            => JsonSerializer.Serialize(writer, (DataList<LinkOr<T>>) value, options);
    }
}
