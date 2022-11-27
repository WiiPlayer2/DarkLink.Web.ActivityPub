using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

internal class UriConverter : LinkedDataConverter<Uri>
{
    protected override Uri? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        => dataList.Value?.Id;

    protected override DataList<LinkedData> ConvertBack(Uri? value, LinkedDataSerializationOptions options)
        => value is null ? default : new LinkedData {Id = value};
}
