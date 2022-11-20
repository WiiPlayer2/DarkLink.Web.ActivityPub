using System.Collections.Immutable;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;

internal class ResourceDescriptorProvider : IResourceDescriptorProvider
{
    public const string USER = "dev2";

    public async Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(Uri resource, IReadOnlyList<string> relations, HttpRequest request, CancellationToken cancellationToken)
    {
        if (resource.Scheme is not "acct")
            return default;
        
        if (!resource.LocalPath.EndsWith("@devtunnel.dark-link.info"))
            return default;

        var username = resource.LocalPath[..^25];

        if (!Directory.Exists("./data"))
            Directory.CreateDirectory("./data");

        if (!Directory.Exists($"./data/{username}"))
        {
            Directory.CreateDirectory($"./data/{username}");
            await File.WriteAllTextAsync($"./data/{username}/intro.txt", $"🧪 Just testing here. [{username}] 🧪", CancellationToken.None);
        }

        var descriptor = JsonResourceDescriptor.Empty with
        {
            Subject = resource,
            Links = ImmutableList.Create(
                Link.Create(Constants.RELATION_PROFILE_PAGE) with
                {
                    Type = "text/html",
                    Href = new Uri($"https://devtunnel.dark-link.info/profiles/{username}"),
                },
                Link.Create("self") with
                {
                    Type = "application/activity+json",
                    Href = new Uri($"https://devtunnel.dark-link.info/profiles/{username}.json"),
                }),
        };
        return descriptor;
    }
}
