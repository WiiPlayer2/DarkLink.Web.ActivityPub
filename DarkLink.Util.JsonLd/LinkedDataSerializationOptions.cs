using DarkLink.Util.JsonLd.Converters;

namespace DarkLink.Util.JsonLd;

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
    }

    public LinkedDataSerializationOptions(LinkedDataSerializationOptions copyFrom)
    {
        Converters = new List<ILinkedDataConverter>(copyFrom.Converters);
    }

    public List<ILinkedDataConverter> Converters { get; }
}
