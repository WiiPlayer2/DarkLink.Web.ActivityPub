using DarkLink.Util.JsonLd;
using DarkLink.Util.JsonLd.Types;
using Microsoft.AspNetCore.Mvc;

namespace DarkLink.Web.ActivityPub.Server.Results;

public class LinkedDataResult<T> : IActionResult
{
    private readonly LinkedDataList<ContextEntry> context;

    private readonly LinkedDataSerializationOptions options;

    private readonly T value;

    public LinkedDataResult(
        T value,
        LinkedDataList<ContextEntry> context,
        LinkedDataSerializationOptions options)
    {
        this.value = value;
        this.context = context;
        this.options = options;
    }

    public Task ExecuteResultAsync(ActionContext context)
        => context.HttpContext.Response.WriteLinkedData(value, this.context, options, context.HttpContext.RequestAborted);
}
