using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Server.Results;

namespace DarkLink.Web.ActivityPub.Server;

public static class ActivityPubResults
{
    public static LinkedDataResult<T> LinkedData<T>(
        T value,
        LinkedDataList<ContextEntry> context,
        LinkedDataSerializationOptions options)
        => new(value, context, options);
}
