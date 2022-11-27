using System.Diagnostics.CodeAnalysis;
using DarkLink.Util.JsonLd.Types;

namespace DarkLink.Util.JsonLd;

public interface ILinkedDataTypeResolver
{
    bool TryResolve(Type targetType, DataList<LinkedData> dataList, [NotNullWhen(true)] out Type? newTargetType);
}
