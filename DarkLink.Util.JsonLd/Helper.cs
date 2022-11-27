using System.Text.Json;
using System.Text.Json.Nodes;

namespace DarkLink.Util.JsonLd;

internal static class Helper
{
    public static TNode? Copy<TNode>(this TNode? source)
        where TNode : JsonNode
        => source is null
            ? null
            : (TNode) JsonNode.Parse(source.ToJsonString())!;

    public static T Create<T>(this Type openGenericType, params Type[] typeArguments)
    {
        var genericType = openGenericType.MakeGenericType(typeArguments);
        return (T) Activator.CreateInstance(genericType)!;
    }

    public static bool TryDeserializeProperty<T>(this JsonObject jsonObject, string propertyName, out T? value, JsonSerializerOptions? options = default)
    {
        value = default;
        if (!jsonObject.TryGetPropertyValue(propertyName, out var propertyNode))
            return false;

        value = propertyNode.Deserialize<T>(options);
        return true;
    }
}
