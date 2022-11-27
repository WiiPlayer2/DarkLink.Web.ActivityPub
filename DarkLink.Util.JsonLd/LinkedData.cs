using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public record LinkedData
{
    public Uri? Id { get; init; }

    public DataList<LinkedData> this[Uri property] => Properties.TryGetValue(property, out var value) ? value : default;

    public IReadOnlyDictionary<Uri, DataList<LinkedData>> Properties { get; init; } = new Dictionary<Uri, DataList<LinkedData>>();

    public DataList<Uri> Type { get; init; }

    public JsonValue? Value { get; init; }
}
