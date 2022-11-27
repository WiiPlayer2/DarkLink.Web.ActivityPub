using System.Diagnostics.CodeAnalysis;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

internal class FallbackTypeResolver : ILinkedDataTypeResolver
{
    public bool TryResolve(Type targetType, DataList<LinkedData> dataList, [NotNullWhen(true)] out Type? newTargetType)
    {
        newTargetType = targetType;
        return true;
    }
}
