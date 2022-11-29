using System.Collections.Immutable;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;
using Microsoft.Extensions.Options;
using APConstants = DarkLink.Web.ActivityPub.Types.Constants;

namespace Demo.Server;

internal class ResourceDescriptorProvider : IResourceDescriptorProvider
{
    private readonly string username;

    public ResourceDescriptorProvider(IOptions<Config> config)
    {
        username = config.Value.Username;
    }

    public Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(Uri resource, IReadOnlyList<string> relations, HttpRequest request, CancellationToken cancellationToken)
    {
        var userUri = new Uri($"acct:{username}@{request.Host}");
        if (!resource.Equals(userUri))
            return Task.FromResult(default(JsonResourceDescriptor));

        var descriptor = JsonResourceDescriptor.Empty with
        {
            Subject = resource,
            Links = ImmutableList.Create(
                Link.Create("self") with
                {
                    Type = APConstants.MEDIA_TYPE,
                    Href = new Uri($"{request.Scheme}://{request.Host}/profile"),
                }),
        };
        return Task.FromResult(descriptor)!;
    }
}
