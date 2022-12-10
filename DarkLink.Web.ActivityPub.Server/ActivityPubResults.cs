using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Server.Results;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;

namespace DarkLink.Web.ActivityPub.Server;

public static class ActivityPubResults
{
    public static LinkedDataResult<T> ActivityPub<T>(
        T value,
        LinkedDataList<ContextEntry> additionalContext = default,
        LinkedDataSerializationOptions? options = default)
        => LinkedData(value, new LinkedDataList<ContextEntry>(Constants.Context.Concat(additionalContext).ToList()), options ?? Constants.SerializationOptions);

    public static LinkedDataResult<T> LinkedData<T>(
        T value,
        LinkedDataList<ContextEntry> context,
        LinkedDataSerializationOptions options)
        => new(value, context, options);
}
