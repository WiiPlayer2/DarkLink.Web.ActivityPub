using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Converters.Json;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public static class LinkedDataSerializer
{
    public static T? Deserialize<T>(JsonNode node, LinkedDataList<ContextEntry> context = default, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);

        var expanded = node.Expand();
        var contextNode = JsonSerializer.SerializeToNode(context, options) ?? new JsonObject();
        var compacted = expanded.Compact(contextNode);
        return compacted.Deserialize<T>(options);
    }

    public static object? DeserializeFromLinkedData(DataList<LinkedData> data, Type targetType, LinkedDataSerializationOptions? options = default)
    {
        options ??= new LinkedDataSerializationOptions();

        var converter = options.Converters.LastOrDefault(c => c.CanConvert(targetType)) ?? throw new InvalidOperationException($"Unable to deserialize type {targetType.AssemblyQualifiedName}.");
        var result = converter.Convert(data, targetType, options);
        return result;
    }

    public static T? DeserializeFromLinkedData<T>(DataList<LinkedData> data, LinkedDataSerializationOptions? options = default)
        => (T?) DeserializeFromLinkedData(data, typeof(T), options);

    public static DataList<LinkedData> DeserializeLinkedData(JsonNode node, JsonSerializerOptions? options = default)
    {
        options = Prepare2(options);

        var expanded = node.Expand().First()!.AsObject();
        var linkedData = expanded.Deserialize<LinkedData>(options);
        return linkedData;
    }

    private static JsonSerializerOptions Prepare<T>(JsonSerializerOptions? options) =>
        new(options ?? new JsonSerializerOptions())
        {
            Converters =
            {
                LinkedDataConverter.Instance,
                DataListConverter.Instance,
                LinkOrConverter.Instance,
                LinkedDataListConverter.Instance,
                ContextEntryConverter.Instance,
                TermMappingConverter.Instance,
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

    private static JsonSerializerOptions Prepare2(JsonSerializerOptions? options) =>
        new(options ?? new JsonSerializerOptions())
        {
            Converters =
            {
                LinkedDataConverter2.Instance,
                LinkOrConverter.Instance,
                DataListConverter.Instance,
                LinkedDataListConverter.Instance,
                ContextEntryConverter.Instance,
                TermMappingConverter.Instance,
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

    public static JsonNode? Serialize<T>(T obj, LinkedDataList<ContextEntry> context = default, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);

        var node = JsonSerializer.SerializeToNode(obj, options);
        if (node is not null)
        {
            var contextNode = JsonSerializer.SerializeToNode(context, options);
            if (contextNode is not null)
            {
                node = node.Compact(contextNode);
                node["@context"] = contextNode;
            }
        }

        return node;
    }

    public static JsonNode? SerializeContext(LinkedDataList<ContextEntry> context, JsonSerializerOptions? options = default)
    {
        options = Prepare2(options);
        return JsonSerializer.SerializeToNode(context, options);
    }

    public static JsonNode SerializeLinkedData(DataList<LinkedData> linkedData, JsonSerializerOptions? options = default)
    {
        options = Prepare2(options);

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
