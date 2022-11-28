using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using Microsoft.AspNetCore.Http;
using Constants = DarkLink.Web.ActivityPub.Types.Constants;

namespace DarkLink.Web.ActivityPub.Server;

public static class Extension
{
    public static ValueTask<T?> ReadLinkedData<T>(
        this HttpRequest request,
        CancellationToken cancellationToken = default)
        => request.ReadLinkedData<T>(new LinkedDataSerializationOptions(), cancellationToken);

    public static async ValueTask<T?> ReadLinkedData<T>(
        this HttpRequest request,
        LinkedDataSerializationOptions options,
        CancellationToken cancellationToken = default)
    {
        var node = await request.ReadFromJsonAsync<JsonNode>(cancellationToken);
        if (node is null)
            return default;

        var value = LinkedDataSerializer.Deserialize<T>(node, options);
        return value;
    }

    public static async Task WriteLinkedData<T>(
        this HttpResponse response,
        T value,
        LinkedDataList<ContextEntry> context,
        LinkedDataSerializationOptions options,
        CancellationToken cancellationToken = default)
    {
        var linkedDataNode = LinkedDataSerializer.Serialize(value, context, options);
        await response.WriteAsJsonAsync(linkedDataNode, options.JsonSerializerOptions, Constants.MEDIA_TYPE, cancellationToken);
    }
}
