using System.Text.Json;
using System.Text.Json.Nodes;

namespace DarkLink.Util.JsonLd;

public class JsonLdSerializer
{
    public T? Deserialize<T>(JsonNode node)
    {
        var expanded = node.Expand();
        var context = new JsonObject();
        var compacted = expanded.Compact(context);
        return compacted.Deserialize<T>(new JsonSerializerOptions
        {
            Converters = {LinkedDataConverter.Instance,},
            PropertyNamingPolicy = LinkedDataNamingPolicy<T>.Instance,
            PropertyNameCaseInsensitive = true,
        });
    }
}
