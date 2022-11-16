using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace DarkLink.Text.Json.NewtonsoftJsonMapper;

public static class Mapper
{
    public static JToken Map(this JsonNode? node)
        => node switch
        {
            JsonObject obj => obj.Map(),
            JsonArray array => array.Map(),
            JsonValue value => value.Map(),
            null => default(JsonValue).Map(),
            _ => throw new NotImplementedException(),
        };

    public static JsonNode? Map(this JToken token)
        => token switch
        {
            JObject obj => obj.Map(),
            JArray array => array.Map(),
            JValue value => value.Map(),
            _ => throw new NotImplementedException(),
        };

    public static JsonArray Map(this JArray array)
        => new(array.Select(Map).ToArray());

    public static JsonObject Map(this JObject obj)
        => new(obj.Properties().ToDictionary(p => p.Name, p => p.Value?.Map()));

    public static JsonValue? Map(this JValue value)
        => JsonValue.Create(value.Value);

    public static JArray Map(this JsonArray array)
        => JArray.FromObject(array.Select(Map));

    public static JObject Map(this JsonObject obj)
        => JObject.FromObject(obj.ToDictionary(p => p.Key, p => p.Value.Map()));

    public static JValue Map(this JsonValue? value)
        => value is null
            ? JValue.CreateNull()
            : (JValue) JToken.Parse(value.ToJsonString());
}
