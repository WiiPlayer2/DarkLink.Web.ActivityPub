using Microsoft.AspNetCore.Mvc.ModelBinding;
using Object = DarkLink.Web.ActivityPub.Types.Object;

namespace DarkLink.Web.ActivityPub.Server;

public class ActivityPubModelBinder : IModelBinder
{
    public async Task BindModelAsync(ModelBindingContext bindingContext)
    {
        var obj = await bindingContext.HttpContext.Request.ReadActivityPub(bindingContext.ModelType, cancellationToken: bindingContext.HttpContext.RequestAborted);
        bindingContext.Result = ModelBindingResult.Success(obj);
    }
}

public class ActivityPubModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
        => context.Metadata.ModelType.IsAssignableTo(typeof(Object)) ? new ActivityPubModelBinder() : null;
}
