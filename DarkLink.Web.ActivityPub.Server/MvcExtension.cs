using Microsoft.Extensions.DependencyInjection;

namespace DarkLink.Web.ActivityPub.Server;

public static class MvcExtension
{
    public static IMvcBuilder AddActivityPub(this IMvcBuilder builder)
        => builder.AddMvcOptions(options => options.ModelBinderProviders.Insert(0, new ActivityPubModelBinderProvider()));
}
