using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

public interface ILinkedDataConverter
{
    bool CanConvert(Type typeToConvert);

    object? Convert(DataList<LinkedData> dataList, Type typeToConvert, LinkedDataSerializationOptions options);

    DataList<LinkedData> ConvertBack(object? value, LinkedDataSerializationOptions options);
}
