using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Converters;
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
}
