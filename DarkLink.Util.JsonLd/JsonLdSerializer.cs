using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Converters;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public class JsonLdSerializer
{
    private static JsonNode CreateContext(Type type)
    {
        var seenTypes = new HashSet<Type>();
        var mappings = GetMappings(type, seenTypes)
            .Distinct()
            .OrderBy(o => o.Property, StringComparer.Ordinal)
            .ToList();
        var context = new JsonObject(mappings.ToDictionary(o => o.Property, o => (JsonNode?) JsonValue.Create(o.Iri)));
        return context;

        static IEnumerable<(string Property, Uri Iri)> GetMappings(Type type, ISet<Type> seenTypes)
        {
            var metadata = type.GetCustomAttribute<LinkedDataAttribute>();
            var proxies = type.GetCustomAttribute<ContextProxyAttribute>();
            if ((metadata is null && proxies is null)
                || !seenTypes.Add(type))
                yield break;

            foreach (var proxyType in type.ResolveContextProxies())
            foreach (var tuple in GetMappings(proxyType, seenTypes))
                yield return tuple;

            if (metadata is not null)
                yield return (type.Name, new Uri(metadata?.Path + type.Name));

            if (proxies is not {IgnoreProperties: true})
                foreach (var propertyInfo in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
                {
                    var propertyMetadata = propertyInfo.GetCustomAttribute<LinkedDataAttribute>();
                    var name = propertyInfo.Name.Uncapitalize();
                    var iri = propertyMetadata is {Path: { }}
                        ? new Uri(propertyMetadata.Path, UriKind.RelativeOrAbsolute)
                        : new Uri(metadata?.Path + name);
                    yield return (name, iri);

                    foreach (var tuple in GetMappings(propertyInfo.PropertyType, seenTypes))
                        yield return tuple;
                }
        }
    }

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
                LinkedDataListConverter.Instance,
                ContextEntryConverter.Instance,
                TermMappingConverter.Instance,
            },
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault,
        };

    public JsonNode? Serialize<T>(T obj, LinkedDataList<ContextEntry> context, JsonSerializerOptions? options = default)
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

    public JsonNode? Serialize<T>(T obj, JsonSerializerOptions? options = default)
    {
        options = Prepare<T>(options);

        var node = JsonSerializer.SerializeToNode(obj, options);
        if (node is not null)
        {
            var context = CreateContext(typeof(T));
            node = node.Compact(context);
        }

        return node;
    }
}
