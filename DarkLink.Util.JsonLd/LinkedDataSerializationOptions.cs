using System.Text.Json;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Converters;
using DarkLink.Util.JsonLd.Converters.Json;

namespace DarkLink.Util.JsonLd;

public class LinkedDataSerializationOptions
{
    public LinkedDataSerializationOptions()
    {
        Converters = new List<ILinkedDataConverter>
        {
            new ObjectConverter(),
            new EnumerableConverter(),
            new PrimitiveConverter(),
            new StringConverter(),
            new UriConverter(),
        };
        TypeResolvers = new List<ILinkedDataTypeResolver>
        {
            new FallbackTypeResolver(),
        };
        DefaultIgnoreCondition = LinkedDataIgnoreCondition.WhenWritingDefault;
        JsonSerializerOptions = new JsonSerializerOptions
        {
            Converters =
            {
                LinkedDataConverter.Instance,
                LinkOrConverter.Instance,
                DataListConverter.Instance,
                LinkedDataListConverter.Instance,
                ContextEntryConverter.Instance,
                TermMappingConverter.Instance,
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };
    }

    public LinkedDataSerializationOptions(LinkedDataSerializationOptions copyFrom)
    {
        Converters = new List<ILinkedDataConverter>(copyFrom.Converters);
        TypeResolvers = new List<ILinkedDataTypeResolver>(copyFrom.TypeResolvers);
        DefaultIgnoreCondition = copyFrom.DefaultIgnoreCondition;
        JsonSerializerOptions = new JsonSerializerOptions(copyFrom.JsonSerializerOptions);
    }

    public List<ILinkedDataConverter> Converters { get; }

    public LinkedDataIgnoreCondition DefaultIgnoreCondition { get; set; }

    public JsonSerializerOptions JsonSerializerOptions { get; }

    public List<ILinkedDataTypeResolver> TypeResolvers { get; }
}
