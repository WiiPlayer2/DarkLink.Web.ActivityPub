using System.Text.Json;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Converters;

namespace DarkLink.Util.JsonLd;

public class JsonLdSerializer
{
    public T? Deserialize<T>(JsonNode node, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);

        var expanded = node.Expand();
        var context = new JsonObject();
        var compacted = expanded.Compact(context);
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
            },
        };

    public JsonNode? Serialize<T>(T obj, JsonNode? context = default, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);
        context ??= new JsonObject();

        var node = JsonSerializer.SerializeToNode(obj, options);
        var compacted = node?.Compact(context);
        return compacted;
        //return node;
    }
}
