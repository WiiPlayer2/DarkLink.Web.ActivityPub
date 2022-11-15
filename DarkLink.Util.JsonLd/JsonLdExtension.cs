using System.Text.Json.Nodes;
using DarkLink.Text.Json.NewtonsoftJsonMapper;
using JsonLD.Core;

namespace DarkLink.Util.JsonLd;

internal static class JsonLdExtension
{
    public static JsonObject Compact(this JsonNode node, JsonNode context)
        => JsonLdProcessor.Compact(node.Map(), context.Map(), new JsonLdOptions()).Map();

    public static JsonArray Expand(this JsonNode node)
        => JsonLdProcessor.Expand(node.Map()).Map();
}
