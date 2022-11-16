namespace DarkLink.Util.JsonLd.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public sealed class LinkedDataAttribute : Attribute
{
    public LinkedDataAttribute(string? path = default, string? type = default)
    {
        Path = path;
        Type = type;
    }

    public string? Path { get; set; }

    public string? Type { get; set; }
}
