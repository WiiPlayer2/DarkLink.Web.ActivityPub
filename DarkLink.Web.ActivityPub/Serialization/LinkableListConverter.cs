using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Converters;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace DarkLink.Web.ActivityPub.Serialization;

public class LinkableListConverter : LinkedDataConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
        => typeToConvert.IsGenericType && typeToConvert.GetGenericTypeDefinition() == typeof(LinkableList<>);

    protected override ILinkedDataConverter? CreateConverter(Type typeToConvert, LinkedDataSerializationOptions options)
        => (ILinkedDataConverter) Activator.CreateInstance(typeof(Conv<>).MakeGenericType(typeToConvert.GenericTypeArguments[0]))!;

    private class Conv<T> : LinkedDataConverter<LinkableList<T>> where T : Object
    {
        protected override LinkableList<T> Convert(DataList<LinkedData> dataList, LinkedDataSerializationOptions options)
            => LinkedDataSerializer.DeserializeFromLinkedData<DataList<LinkTo<T>>>(dataList, options);

        protected override DataList<LinkedData> ConvertBack(LinkableList<T> value, LinkedDataSerializationOptions options)
            => LinkedDataSerializer.SerializeToLinkedData((DataList<LinkTo<T>>) value, options: options);
    }
}
