using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public interface ILinkedDataConverter
{
    bool CanConvert(Type typeToConvert);

    object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options);

    DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options);
}

public class PrimitiveConverter : LinkedDataConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsPrimitive;

    protected override ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options)
        => typeof(Conv<>).Create<ILinkedDataConverter>(typeToConvert);

    private class Conv<T> : LinkedDataConverter<T>
        where T : struct
    {
        protected override T Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
            => dataList.Value?.Value?.GetValue<T>() ?? default;

        protected override DataList<LinkedData> ConvertBack(T value, LinkedDataSerializationOptions options)
            => new LinkedData
            {
                Value = JsonValue.Create(value),
            };
    }
}
