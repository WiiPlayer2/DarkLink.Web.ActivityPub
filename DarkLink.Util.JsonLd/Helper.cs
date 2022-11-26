using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Attributes;

namespace DarkLink.Util.JsonLd;

internal static class Helper
{
    public static T Create<T>(this Type openGenericType, params Type[] typeArguments)
    {
        var genericType = openGenericType.MakeGenericType(typeArguments);
        return (T) Activator.CreateInstance(genericType)!;
    }

    public static IEnumerable<Type> ResolveContextProxies(this Type type)
    {
        var contextProxyAttribute = type.GetCustomAttribute<ContextProxyAttribute>();

        if (contextProxyAttribute is null)
            yield break;

        foreach (var proxyType in contextProxyAttribute.ProxyTypes)
            yield return proxyType;

        if (contextProxyAttribute.ProxyTypeResolver is not null)
        {
            var resolver = (IContextProxyResolver) Activator.CreateInstance(contextProxyAttribute.ProxyTypeResolver)!;
            foreach (var proxyType in resolver.ResolveProxyTypes(type))
                yield return proxyType;
        }
    }

    public static bool TryDeserializeProperty<T>(this JsonObject jsonObject, string propertyName, out T? value, JsonSerializerOptions? options = default)
    {
        value = default;
        if (!jsonObject.TryGetPropertyValue(propertyName, out var propertyNode))
            return false;

        value = propertyNode.Deserialize<T>(options);
        return true;
    }

    public static string Uncapitalize(this string s)
        => string.IsNullOrEmpty(s)
            ? string.Empty
            : $"{char.ToLowerInvariant(s[0])}{s[1..]}";
}

public interface IContextProxyResolver
{
    IEnumerable<Type> ResolveProxyTypes(Type proxiedType);
}
