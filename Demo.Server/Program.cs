using System;
using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;
using JsonLD.Util;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();

var app = builder.Build();
app.UseWebFinger();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/profile", () => "Welcome to my profile.");

app.MapGet("/profile.json", async ctx =>
{
    var person = new Person(
        new("https://devtunnel.dark-link.info/profile.json"),
        new("https://www.w3.org/ns/activitystreams#Person"),
        "me",
        "Waldemar Tomme",
        "Me testing here.",
        new("https://devtunnel.dark-link.info/profile"),
        new Image[]
        {
            new(
                new("https://www.w3.org/ns/activitystreams#Image"),
                "image/png",
                new("https://assets.tech.lgbt/accounts/avatars/109/318/341/050/998/934/original/4bee8ed06d7c83b9.png")),
        });

    var context = new JsonArray(JsonValue.Create("https://www.w3.org/ns/activitystreams"));
    var node = new JsonLdSerializer().Serialize(person);

    ctx.Response.Headers.ContentType = "application/activity+json";
    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);
});

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

[LinkedData("https://www.w3.org/ns/activitystreams#")]
internal record Person(
    Uri Id,
    Uri Type,
    string PreferredUsername,
    string Name,
    string Summary,
    Uri Url,
    IReadOnlyList<Image> Icon);

[LinkedData("https://www.w3.org/ns/activitystreams#")]
internal record Image(
    Uri Type,
    string MediaType,
    Uri Url);