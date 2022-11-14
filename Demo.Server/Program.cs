using System.Collections.Immutable;
using System.Net;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();

var app = builder.Build();
app.UseWebFinger();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/profile", () => "Welcome to my profile.");

app.MapMethods(
    "/{*path}",
    new[] {HttpMethods.Get, HttpMethods.Post,},
    async ctx =>
    {
        var headers = string.Join('\n', ctx.Request.Headers.Select(h => $"{h.Key}: {h.Value}"));
        var query = string.Join('\n', ctx.Request.Query.Select(q => $"{q.Key}={q.Value}"));
        using var reader = new StreamReader(ctx.Request.Body);
        var body = await reader.ReadToEndAsync();
        logger.LogDebug($"{ctx.Request.Method} {ctx.Request.GetDisplayUrl()}\n{query}\n{headers}\n{body}");
        ctx.Response.StatusCode = (int) HttpStatusCode.InternalServerError;
        await ctx.Response.CompleteAsync();
    });

app.Run();

internal class ResourceDescriptorProvider : IResourceDescriptorProvider
{
    public Task<JsonResourceDescriptor?> GetResourceDescriptorAsync(Uri resource, IReadOnlyList<string> relations, HttpRequest request, CancellationToken cancellationToken)
    {
        if (resource != new Uri("acct:me@devtunnel.dark-link.info"))
            return Task.FromResult(default(JsonResourceDescriptor?));

        var descriptor = JsonResourceDescriptor.Empty with
        {
            Subject = new Uri("acct:me@devtunnel.dark-link.info"),
            Links = ImmutableList.Create(
                Link.Create("http://webfinger.net/rel/profile-page") with
                {
                    Type = "text/html",
                    Href = new Uri("https://devtunnel.dark-link.info/profile"),
                }),
        };
        return Task.FromResult<JsonResourceDescriptor?>(descriptor);
    }
}
