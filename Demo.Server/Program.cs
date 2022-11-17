using System.Net;
using System.Text.Json.Nodes;
using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using DarkLink.Web.ActivityPub.Types.Extended;
using DarkLink.Web.WebFinger.Server;
using Microsoft.AspNetCore.Http.Extensions;
using ASLink = DarkLink.Web.ActivityPub.Types.Link;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddWebFinger<ResourceDescriptorProvider>();

var app = builder.Build();
app.UseWebFinger();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapGet("/profile", () => "Welcome to my profile.");

app.MapGet("/profile.png", async ctx => { await ctx.Response.SendFileAsync("./profile.png", ctx.RequestAborted); });

app.MapGet("/profile.json", async ctx =>
{
    ctx.Response.Headers.CacheControl = "max-age=0, private, must-revalidate";
    ctx.Response.Headers.ContentType = "application/activity+json; charset=utf-8";

    var person = new Person(new Uri("https://devtunnel.dark-link.info/inbox"), new Uri("https://devtunnel.dark-link.info/outbox"))
    {
        Id = new Uri("https://devtunnel.dark-link.info/profile.json"),
        PreferredUsername = ResourceDescriptorProvider.USER,
        Name = "Waldemar Tomme (DEV)",
        Summary = "Just testing around 🧪",
        Url = DataList.From<LinkOr<ASLink>>(new Link<ASLink>(new Uri("https://devtunnel.dark-link.info/profile"))),
        Icon = DataList.From<LinkOr<Image>>(new Object<Image>(new Image
        {
            MediaType = "image/png",
            Url = DataList.From<LinkOr<ASLink>>(new Link<ASLink>(new Uri("https://devtunnel.dark-link.info/profile.png"))),
        })),
    };

    //var context = new JsonArray(JsonValue.Create("https://www.w3.org/ns/activitystreams"));
    var context = JsonNode.Parse(@"[
""https://www.w3.org/ns/activitystreams"",
{
""url"":""as:url"",
""inbox"":""as:inbox"",
""outbox"":""as:outbox""
}
]");
    var node = new JsonLdSerializer().Serialize(person, context);

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
