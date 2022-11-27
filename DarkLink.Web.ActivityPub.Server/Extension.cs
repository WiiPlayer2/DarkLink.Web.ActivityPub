using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd;
using Microsoft.AspNetCore.Http;

namespace DarkLink.Web.ActivityPub.Server;

public static class Extension
{
    public static ValueTask<T?> ReadLinkedData<T>(
        this HttpRequest request,
        CancellationToken cancellationToken = default)
        => request.ReadLinkedData<T>(new(), cancellationToken);

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
}
