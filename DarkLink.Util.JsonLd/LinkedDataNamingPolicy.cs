using System;
using System.Reflection;
using System.Text.Json;

namespace DarkLink.Util.JsonLd;

internal class LinkedDataNamingPolicy<T> : JsonNamingPolicy
{
    private readonly string basePath;

    private LinkedDataNamingPolicy()
    {
        basePath = typeof(T).GetCustomAttribute<LinkedDataAttribute>()?.Path ?? string.Empty;
    }

    public static LinkedDataNamingPolicy<T> Instance { get; } = new();

    public override string ConvertName(string name)
        => name.ToLowerInvariant() switch
        {
            "id" => "@id",
            "type" => "@type",
            "value" => "@value",
            _ => GetPropertyName(name),
        };

    private string GetPropertyName(string property)
    {
        var propertyName = typeof(T).GetProperty(property)?.GetCustomAttribute<LinkedDataAttribute>()?.Path ?? Uncapitalize(property);
        var fullName = $"{basePath}{propertyName}";
        return fullName;
    }

    private string Uncapitalize(string s)
        => s == string.Empty
            ? string.Empty
            : $"{char.ToLowerInvariant(s[0])}{s[1..]}";
}
