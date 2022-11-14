using DarkLink.Web.WebFinger.Shared;
using Microsoft.AspNetCore.Http;

namespace DarkLink.Web.WebFinger.Server;

public interface IResourceDescriptorProvider
{
    Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(Uri resource, IReadOnlyList<string> relations, HttpRequest request, CancellationToken cancellationToken);
}
