using System.Collections.Immutable;
using System.Net;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Attributes;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.WebFinger.Server;
using DarkLink.Web.WebFinger.Shared;
using Microsoft.AspNetCore.Http.Extensions;

const string USER = ResourceDescriptorProvider.USER;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();

var app = builder.Build();
app.UseWebFinger();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/profile", () => "Welcome to my profile.");

app.MapGet("/profile.json", async ctx =>
{
    ctx.Response.Headers.CacheControl = "max-age=0, private, must-revalidate";
    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";

    var person = new Person(
        new Uri("https://devtunnel.dark-link.info/profile.json"),
        new Uri("https://www.w3.org/ns/activitystreams#Person"),
        new Uri("https://devtunnel.dark-link.info/inbox"),
        new Uri("https://devtunnel.dark-link.info/outbox"),
        USER,
        "Waldemar Tomme",
        "Me testing here.",
        new Uri("https://devtunnel.dark-link.info/profile"),
        DataList.FromItems(new Image[]
        {
            new(
                new Uri("https://www.w3.org/ns/activitystreams#Image"),
                "image/png",
                new Uri("https://assets.tech.lgbt/accounts/avatars/109/318/341/050/998/934/original/4bee8ed06d7c83b9.png")),
        }));

    //var context = new JsonArray(JsonValue.Create("https://www.w3.org/ns/activitystreams"));
    var context = JsonNode.Parse(@"[
""https://www.w3.org/ns/activitystreams"",
""https://w3id.org/security/v1"",
{
""Hashtag"":""as:Hashtag"",
""sensitive"":""as:sensitive"",
""manuallyApprovesFollowers"":""as:manuallyApprovesFollowers"",
""alsoKnownAs"":{""@id"":""as:alsoKnownAs"",""@type"":""@id""},
""movedTo"":{""@id"":""as:movedTo"",""@type"":""@id""},
""toot"":""http://joinmastodon.org/ns#"",
""featured"":{""@id"":""toot:featured"",""@type"":""@id""},
""Emoji"":""toot:Emoji"",
""blurhash"":""toot:blurhash"",
""votersCount"":""toot:votersCount"",
""schema"":""http://schema.org#"",
""PropertyValue"":""schema:PropertyValue"",
""value"":""schema:value"",
""ostatus"":""http://ostatus.org#"",
""conversation"":""ostatus:conversation"",
""url"":""as:url"",
""inbox"":""as:inbox"",
""outbox"":""as:outbox""
}
]");
    var node = new JsonLdSerializer().Serialize(person, context);

    await ctx.Response.WriteAsync(node?.ToString() ?? string.Empty, ctx.RequestAborted);

    //    var json = @"{
    //""@context"":[""https://www.w3.org/ns/activitystreams"",""https://w3id.org/security/v1"",{""Hashtag"":""as:Hashtag"",""sensitive"":""as:sensitive"",""manuallyApprovesFollowers"":""as:manuallyApprovesFollowers"",""alsoKnownAs"":{""@id"":""as:alsoKnownAs"",""@type"":""@id""},""movedTo"":{""@id"":""as:movedTo"",""@type"":""@id""},""toot"":""http://joinmastodon.org/ns#"",""featured"":{""@id"":""toot:featured"",""@type"":""@id""},""Emoji"":""toot:Emoji"",""blurhash"":""toot:blurhash"",""votersCount"":""toot:votersCount"",""schema"":""http://schema.org#"",""PropertyValue"":""schema:PropertyValue"",""value"":""schema:value"",""ostatus"":""http://ostatus.org#"",""conversation"":""ostatus:conversation""}],
    //""type"":""Person"",
    //""id"":""https://devtunnel.dark-link.info/profile.json"",
    //""inbox"":""https://devtunnel.dark-link.info/inbox"",
    //""outbox"":""https://devtunnel.dark-link.info/outbox"",
    //""preferredUsername"":""" + USER + @""",
    //""name"":""testing2"",
    //""summary"":""just testing here"",
    //""url"":""https://devtunnel.dark-link.info/profile"",
    //""icon"":{""mediaType"":""image/png"",""type"":""Image"",""url"":""https://assets.tech.lgbt/accounts/avatars/109/318/341/050/998/934/original/4bee8ed06d7c83b9.png""}
    //}";
    //    await ctx.Response.WriteAsync(json, ctx.RequestAborted);
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
    public const string USER = "dev9";

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

[LinkedData("https://www.w3.org/ns/activitystreams#")]
internal record Person(
    Uri Id,
    Uri Type,
    Uri Inbox,
    Uri Outbox,
    string PreferredUsername,
    string Name,
    string Summary,
    Uri Url,
    DataList<Image> Icon);

[LinkedData("https://www.w3.org/ns/activitystreams#")]
internal record Image(
    Uri Type,
    string MediaType,
    Uri Url);
