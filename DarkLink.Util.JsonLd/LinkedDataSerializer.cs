using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Converters.Json;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public record LinkedData
{
    public Uri? Id { get; init; }

    public IReadOnlyDictionary<Uri, IReadOnlyList<LinkedData>> Properties { get; init; } = new Dictionary<Uri, IReadOnlyList<LinkedData>>();

    public DataList<Uri> Types { get; init; }

    public JsonValue? Value { get; init; }
}

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
        obj.TryDeserializeProperty<DataList<Uri>>("@type", out var types, options);
        obj.TryDeserializeProperty<JsonValue>("@value", out var value, options);

        var keyValuePairs = obj
            .OrderBy(o => o.Key)
            .Where(kv => !keywords.Contains(kv.Key))
            .Select(kv => (new Uri(kv.Key), kv.Value.Deserialize<IReadOnlyList<LinkedData>>(options)!)) // if null then data is illegal
            .ToList();
        var properties = keyValuePairs
            .ToDictionary(pair => pair.Item1, pair => pair.Item2, UriEqualityComparer.Default);

        var linkedData = new LinkedData
        {
            Id = id,
            Types = types,
            Value = value,
            Properties = properties,
        };
        return linkedData;
    }

    public override void Write(Utf8JsonWriter writer, LinkedData value, JsonSerializerOptions options)
    {
        var idNode = JsonSerializer.SerializeToNode(value.Id, options);
        var typesNode = JsonSerializer.SerializeToNode(value.Types, options);
        var properties = value.Properties
            .ToDictionary(
                kv => kv.Key.ToString(),
                kv => JsonSerializer.SerializeToNode(kv.Value, options));

        properties["@id"] = idNode;
        properties["@type"] = typesNode;
        properties["@value"] = value.Value.Copy();
        var obj = new JsonObject(properties.Where(kv => kv.Value is not null));

        JsonSerializer.Serialize(writer, obj, options);
    }

    private class UriEqualityComparer : EqualityComparer<Uri>
    {
        public new static UriEqualityComparer Default { get; } = new();

        public override bool Equals(Uri? x, Uri? y)
            => object.Equals(x, y) && Equals(x?.Fragment, y?.Fragment);

        public override int GetHashCode(Uri obj) => obj.GetHashCode() ^ obj.Fragment.GetHashCode();
    }
}

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

    public static LinkedData? DeserializeLinkedData(JsonNode node, JsonSerializerOptions? options = default)
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

    public static JsonNode SerializeLinkedData(LinkedData linkedData, JsonSerializerOptions? options = default)
    {
        options = Prepare2(options);

        var node = JsonSerializer.SerializeToNode(linkedData, options)!;
        return node;
    }
}
