using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Converters;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace DarkLink.Web.ActivityPub.Serialization;

public class LinkToConverter : LinkedDataConverterFactory
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkTo<>);

    protected override ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options)
    {
        var itemType = typeToConvert.GenericTypeArguments[0];
        var converterType = typeof(Conv<>).MakeGenericType(itemType);
        var converter = (ILinkedDataConverter) Activator.CreateInstance(converterType)!;
        return converter;
    }

    private class Conv<T> : LinkedDataConverter<LinkTo<T>>
        where T : Object
    {
        protected override LinkTo<T>? Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
        {
            var data = dataList.Value;
            if (data is null)
                return null;

            if (data.Type.IsEmpty)
                return data.Id!;

            return LinkedDataSerializer.DeserializeFromLinkedData<T>(data, options)!;
        }

        protected override DataList<LinkedData> ConvertBack(LinkTo<T>? value, LinkedDataSerializationOptions options)
        {
            if (value is null)
                return default;

            // TODO Differentiate between only link and link object
            return value.Match(
                link => LinkedDataSerializer.SerializeToLinkedData(link.Id, options: options),
                obj => LinkedDataSerializer.SerializeToLinkedData(obj, options: options));
        }
    }
}
