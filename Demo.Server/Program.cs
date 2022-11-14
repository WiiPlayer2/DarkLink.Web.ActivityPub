using System.Net;
using Microsoft.AspNetCore.Http.Extensions;

var builder = WebApplication.CreateBuilder(args);

var app = builder.Build();
var logger = app.Services.GetRequiredService<ILogger<Program>>();

app.MapMethods(
    "/{*path}",
    new[] { HttpMethods.Get, HttpMethods.Post },
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
