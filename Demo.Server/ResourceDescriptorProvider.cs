using System.Collections.Immutable;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;

internal class ResourceDescriptorProvider : IResourceDescriptorProvider
{
    public const string USER = "me";

    public Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(Uri resource, IReadOnlyList<string> relations, HttpRequest request, CancellationToken cancellationToken)
    {
        if (resource != new Uri($"acct:{USER}@devtunnel.dark-link.info"))
            return Task.FromResult(default(JsonResourceDescriptor?));

        var descriptor = JsonResourceDescriptor.Empty with
        {
            Subject = new Uri($"acct:{USER}@devtunnel.dark-link.info"),
            Links = ImmutableList.Create(
                Link.Create(Constants.RELATION_PROFILE_PAGE) with
                {
                    Type = "text/html",
                    Href = new Uri("https://devtunnel.dark-link.info/profile"),
                },
                Link.Create("self") with
                {
                    Type = "application/activity+json",
                    Href = new Uri("https://devtunnel.dark-link.info/profile.json"),
                }),
        };
        return Task.FromResult<JsonResourceDescriptor?>(descriptor);
    }
}
