namespace DarkLink.Util.JsonLd;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class LinkedDataAttribute : Attribute
{
    public LinkedDataAttribute(string? basePath = default)
    {
        BasePath = basePath;
    }

    public string? BasePath { get; }
}
