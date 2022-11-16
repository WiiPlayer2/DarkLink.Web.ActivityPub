namespace DarkLink.Util.JsonLd.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public sealed class LinkedDataAttribute : Attribute
{
    public LinkedDataAttribute(string? path = default)
    {
        Path = path;
    }

    public string? Path { get; }
}
