using System;
using System.Text.Json;
using System.Text.Json.Nodes;

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
                LinkOrConverter.Instance,
            },
            PropertyNamingPolicy = LinkedDataNamingPolicy<T>.Instance,
            PropertyNameCaseInsensitive = true,
        };

    public JsonNode? Serialize<T>(T obj, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);

        return JsonSerializer.SerializeToNode(obj, options);
    }
}
