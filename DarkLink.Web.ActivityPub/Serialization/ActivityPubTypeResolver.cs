using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Web.ActivityPub.Serialization;

public class ActivityPubTypeResolver : ILinkedDataTypeResolver
{
    private readonly IReadOnlyList<(Type Type, HashSet<Uri> Types)> types;

    public ActivityPubTypeResolver()
    {
        types = typeof(ActivityPubTypeResolver).Assembly
            .GetExportedTypes()
            .Select(t => (Type: t, Types: t.GetCustomAttributes<LinkedDataTypeAttribute>()
                .Select(a => a.Type)
                .ToHashSet(FullUriEqualityComparer.Default)))
            .Where(pair => pair.Types.Any())
            .ToList();
    }

    public bool TryResolve(Type targetType, DataList<LinkedData> dataList, [NotNullWhen(true)] out Type? newTargetType)
    {
        newTargetType = default;
        if (!dataList.IsValue || dataList.IsEmpty)
            return false;

        var pair = types.FirstOrDefault(pair => pair.Types.IsSubsetOf(dataList.Value!.Type));
        if (pair.Type is null)
            return false;

        if (!targetType.IsAssignableFrom(pair.Type))
            return false;

        newTargetType = pair.Type;
        return true;
    }
}
