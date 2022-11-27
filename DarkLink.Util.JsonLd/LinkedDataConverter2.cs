using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public class LinkedDataConverter2 : JsonConverter<LinkedData>
{
    private static readonly string[] keywords =
    {
        "@id",
        "@type",
        "@value",
    };

    private LinkedDataConverter2() { }

    public static LinkedDataConverter2 Instance { get; } = new();

    public override LinkedData? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = JsonSerializer.Deserialize<JsonObject>(ref reader, options);
        if (obj is null)
            return null;

        obj.TryDeserializeProperty<Uri>("@id", out var id, options);
        obj.TryDeserializeProperty<DataList<Uri>>("@type", out var type, options);
        obj.TryDeserializeProperty<JsonValue>("@value", out var value, options);

        var keyValuePairs = obj
            .OrderBy(o => o.Key)
            .Where(kv => !keywords.Contains(kv.Key))
            .Select(kv => (new Uri(kv.Key), kv.Value.Deserialize<IReadOnlyList<LinkedData>>(options)!)) // if null then data is illegal
            .ToList();
        var properties = keyValuePairs
            .ToDictionary(pair => pair.Item1, pair => DataList.FromItems(pair.Item2), UriEqualityComparer.Default);

        var linkedData = new LinkedData
        {
            Id = id,
            Type = type,
            Value = value,
            Properties = properties,
        };
        return linkedData;
    }

    public override void Write(Utf8JsonWriter writer, LinkedData value, JsonSerializerOptions options)
    {
        var idNode = JsonSerializer.SerializeToNode(value.Id, options);
        var typesNode = JsonSerializer.SerializeToNode(value.Type, options);
        var properties = value.Properties
            .ToDictionary(
                kv => kv.Key.ToString(),
                kv => JsonSerializer.SerializeToNode(new List<LinkedData>(kv.Value), options));

        properties["@id"] = idNode;
        properties["@type"] = typesNode;
        properties["@value"] = value.Value.Copy();
        var obj = new JsonObject(properties.Where(kv => kv.Value is not null));

        JsonSerializer.Serialize(writer, obj, options);
    }
}
