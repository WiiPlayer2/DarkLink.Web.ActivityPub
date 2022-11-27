using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public abstract class LinkedDataConverterFactory : ILinkedDataConverter
{
    public abstract bool CanConvert(Type typeToConvert);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options)
        => CreateConverter(typeToConvert, options)?.Convert(dataList, typeToConvert, options);

    public DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options)
        => CreateConverter(typeToConvert, options)?.ConvertBack(value, typeToConvert, options) ?? default;

    protected abstract ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options);
}
