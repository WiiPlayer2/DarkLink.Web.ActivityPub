using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using DarkLink.Web.WebFinger.Shared;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace DarkLink.Web.WebFinger.Server;

public static class WebApiExtension
{
    private const string WEBFINGER_MEDIATYPE = "application/jrd+json";

    private const string WEBFINGER_PATH = "/.well-known/webfinger";

    public static void AddWebFinger<TResourceDescriptorProvider>(this IServiceCollection services)
        where TResourceDescriptorProvider : class, IResourceDescriptorProvider
    {
        services.AddSingleton<IResourceDescriptorProvider, TResourceDescriptorProvider>();
    }

    public static void UseWebFinger(this IApplicationBuilder app)
    {
        var resourceDescriptorProvider = app.ApplicationServices.GetRequiredService<IResourceDescriptorProvider>();
        app.Map(
            WEBFINGER_PATH,
            app => app.Run(async ctx =>
            {
                if (!ctx.Request.Query.TryGetValue("resource", out var resourceRaw)
                    || !Uri.TryCreate(resourceRaw, UriKind.RelativeOrAbsolute, out var resource))
                {
                    ctx.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    return;
                }

                ctx.Request.Query.TryGetValue("rel", out var relationsRaw);
                var relations = relationsRaw.ToArray();

                var descriptor = await resourceDescriptorProvider.GetResourceDescriptorAsync(resource, relations, ctx.Request, ctx.RequestAborted);
                if (descriptor is null)
                {
                    ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
                }
                else
                {
                    ctx.Response.Headers.ContentType = WEBFINGER_MEDIATYPE;
                    await ctx.Response.WriteAsJsonAsync(
                        descriptor,
                        new JsonSerializerOptions
                        {
                            Converters =
                            {
                                new JsonResourceDescriptorConverter(),
                            },
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        },
                        ctx.RequestAborted);
                }
            }));
    }
}
