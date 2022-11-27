using System.Text.Json;
using DarkLink.Util.JsonLd.Converters;

namespace DarkLink.Util.JsonLd;

public enum LinkedDataIgnoreCondition
{
    Never,

    Always,

    //WhenWritingNull, // TODO I don't think this makes sense for now
    WhenWritingDefault,
}

public class LinkedDataSerializationOptions
{
    public LinkedDataSerializationOptions()
    {
        Converters = new List<ILinkedDataConverter>
        {
            new ObjectConverter(),
            new EnumerableConverter(),
            new StringConverter(),
            new UriConverter(),
        };
        DefaultIgnoreCondition = LinkedDataIgnoreCondition.WhenWritingDefault;
        JsonSerializerOptions = new JsonSerializerOptions();
    }

    public LinkedDataSerializationOptions(LinkedDataSerializationOptions copyFrom)
    {
        Converters = new List<ILinkedDataConverter>(copyFrom.Converters);
        DefaultIgnoreCondition = copyFrom.DefaultIgnoreCondition;
        JsonSerializerOptions = new JsonSerializerOptions(copyFrom.JsonSerializerOptions);
    }

    public List<ILinkedDataConverter> Converters { get; }

    public LinkedDataIgnoreCondition DefaultIgnoreCondition { get; set; }

    public JsonSerializerOptions JsonSerializerOptions { get; }
}
