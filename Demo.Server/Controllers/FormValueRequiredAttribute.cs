using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ActionConstraints;

namespace Demo.Server.Controllers;

[AttributeUsage(AttributeTargets.Method)]
internal sealed class FormValueRequiredAttribute : ActionMethodSelectorAttribute
{
    private readonly string name;

    public FormValueRequiredAttribute(string name)
    {
        this.name = name;
    }

    public override bool IsValidForRequest(RouteContext routeContext, ActionDescriptor action)
        => routeContext.HttpContext.Request.HasFormContentType
           && !string.IsNullOrEmpty(routeContext.HttpContext.Request.Form[name]);
}
