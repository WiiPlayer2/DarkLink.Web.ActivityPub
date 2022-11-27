using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd.Converters;

internal class StringConverter : LinkedDataConverter<string>
{
    protected override string? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        => dataList.Value?.Value?.GetValue<string>();

    protected override DataList<LinkedData> ConvertBack(string? value, LinkedDataSerializationOptions options)
        => value is null
            ? default
            : new LinkedData
            {
                Value = JsonValue.Create(value),
            };
}
