using System.Net.Http.Json;
using System.Text.Json;
using DarkLink.Web.WebFinger.Shared;

namespace DarkLink.Web.WebFinger.Client;

public static class HttpClientWebFingerExtension
{
    private static Uri BuildServerUri(string host)
        => new UriBuilder("https", host, 443).Uri;

    public static Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(this HttpClient httpClient, string host, Uri resource, CancellationToken cancellationToken = default)
        => httpClient.GetResourceDescriptorAsync(
            BuildServerUri(host),
            resource,
            cancellationToken);

    public static Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(this HttpClient httpClient, Uri server, Uri resource, CancellationToken cancellationToken = default)
        => httpClient.GetResourceDescriptorAsync(server, resource, Array.Empty<string>(), cancellationToken);

    public static Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(this HttpClient httpClient, string host, Uri resource, IReadOnlyList<string> relations, CancellationToken cancellationToken = default)
        => httpClient.GetResourceDescriptorAsync(
            BuildServerUri(host),
            resource,
            relations,
            cancellationToken);

    public static async Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(this HttpClient httpClient, Uri server, Uri resource, IReadOnlyList<string> relations, CancellationToken cancellationToken = default)
    {
        var queryString = $"{Constants.QUERY_RESOURCE}={Uri.EscapeDataString(resource.ToString())}";
        queryString += string.Concat(relations.Select(relation => $"&{Constants.QUERY_RELATION}={Uri.EscapeDataString(relation)}"));

        var requestUri = new UriBuilder(server.Scheme, server.Host, server.Port, Constants.HTTP_PATH)
        {
            Query = queryString,
        }.Uri;
        var descriptor = await httpClient.GetFromJsonAsync<JsonResourceDescriptor>(
            requestUri,
            new JsonSerializerOptions
            {
                Converters =
                {
                    new JsonResourceDescriptorConverter(),
                },
            },
            cancellationToken);
        return descriptor;
    }
}
