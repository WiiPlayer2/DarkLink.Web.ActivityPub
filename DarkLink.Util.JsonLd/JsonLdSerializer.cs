using System;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DarkLink.Util.JsonLd;

public class JsonLdSerializer
{
    public T? Deserialize<T>(JsonNode node, JsonSerializerOptions? options = default)
    {
        options ??= new JsonSerializerOptions();
        options = new JsonSerializerOptions(options)
        {
            Converters =
            {
                LinkedDataConverter.Instance,
                LinkOrConverter.Instance,
            },
            PropertyNamingPolicy = LinkedDataNamingPolicy<T>.Instance,
            PropertyNameCaseInsensitive = true,
        };

        var expanded = node.Expand();
        var context = new JsonObject();
        var compacted = expanded.Compact(context);
        return compacted.Deserialize<T>(options);
    }
}
