using System.Text.Json;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public static class LinkedDataSerializer
{
    public static T? Deserialize<T>(JsonNode node, LinkedDataSerializationOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions();

        var expanded = node.Expand();
        var linkedData = DeserializeLinkedData(expanded, options.JsonSerializerOptions);
        var value = DeserializeFromLinkedData<T>(linkedData, options);
        return value;
    }

    public static object? DeserializeFromLinkedData(DataList<LinkedData> data, Type targetType, LinkedDataSerializationOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions();

        foreach (var typeResolver in options.TypeResolvers.AsEnumerable().Reverse())
        {
            if (!typeResolver.TryResolve(targetType, data, out var newTargetType))
                continue;

            targetType = newTargetType;
            break;
        }

        var converter = options.Converters.LastOrDefault(c => c.CanConvert(targetType)) ?? throw new InvalidOperationException($"Unable to deserialize type {targetType.AssemblyQualifiedName}.");
        var result = converter.Convert(data, targetType, options);
        return result;
    }

    public static T? DeserializeFromLinkedData<T>(DataList<LinkedData> data, LinkedDataSerializationOptions? options = default)
        => (T?) DeserializeFromLinkedData(data, typeof(T), options);

    public static DataList<LinkedData> DeserializeLinkedData(JsonNode node, JsonSerializerOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions().JsonSerializerOptions;

        var expanded = node.Expand().First()!.AsObject();
        var linkedData = expanded.Deserialize<LinkedData>(options);
        return linkedData;
    }

    public static JsonNode? Serialize<T>(T? value, LinkedDataList<ContextEntry> context = default, LinkedDataSerializationOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions();

        var linkedData = SerializeToLinkedData(value, typeof(T), options);
        var expanded = SerializeLinkedData(linkedData, options.JsonSerializerOptions);
        var contextNode = SerializeContext(context, options.JsonSerializerOptions);
        var compactNode = expanded.Compact(contextNode ?? new JsonObject());
        return compactNode;
    }

    public static JsonNode? SerializeContext(LinkedDataList<ContextEntry> context, JsonSerializerOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions().JsonSerializerOptions;

        return JsonSerializer.SerializeToNode(context, options);
    }

    public static JsonNode SerializeLinkedData(DataList<LinkedData> linkedData, JsonSerializerOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions().JsonSerializerOptions;

        var node = JsonSerializer.SerializeToNode(linkedData, options)!;
        return node;
    }

    public static DataList<LinkedData> SerializeToLinkedData(object? value, Type? targetType = default, LinkedDataSerializationOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions();

        targetType ??= value?.GetType() ?? typeof(object);
        var converter = options.Converters.LastOrDefault(c => c.CanConvert(targetType)) ?? throw new InvalidOperationException($"Unable to serialize type {targetType.AssemblyQualifiedName}.");
        var result = converter.ConvertBack(value, targetType, options);
        return result;
    }
}
