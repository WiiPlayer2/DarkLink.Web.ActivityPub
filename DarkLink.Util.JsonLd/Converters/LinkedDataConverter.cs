using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public abstract class LinkedDataConverter<T> : ILinkedDataConverter
    where T : notnull
{
    public bool CanConvert(Type typeToConvert) => typeToConvert == typeof(T);

    public object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options) => Convert(dataList, options);

    public DataList<LinkedData> ConvertBack(object? value, Type typeToConvert, LinkedDataSerializationOptions options) => ConvertBack((T) value!, options);

    protected abstract T? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options);

    protected abstract DataList<LinkedData> ConvertBack(T? value, LinkedDataSerializationOptions options);
}
