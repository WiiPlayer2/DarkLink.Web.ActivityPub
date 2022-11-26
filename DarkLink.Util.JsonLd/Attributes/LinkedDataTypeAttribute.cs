namespace DarkLink.Util.JsonLd.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
public sealed class LinkedDataTypeAttribute : Attribute
{
    public LinkedDataTypeAttribute(string type)
    {
        Type = new Uri(type);
    }

    public Uri Type { get; }
}
