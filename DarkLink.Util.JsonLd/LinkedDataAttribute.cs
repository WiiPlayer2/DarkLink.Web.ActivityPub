using System;

namespace DarkLink.Util.JsonLd;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property, Inherited = false)]
public sealed class LinkedDataAttribute : Attribute
{
    public LinkedDataAttribute(string? path = default)
    {
        Path = path;
    }

    public string? Path { get; }
}
