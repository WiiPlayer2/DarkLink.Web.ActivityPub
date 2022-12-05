using System.Security.Claims;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server;

namespace Demo.Server;

internal class TokenRequestHandler : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    private readonly Config config;

    public TokenRequestHandler(IOptions<Config> config)
    {
        this.config = config.Value;
    }

    public ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        if (!context.Request.IsPasswordGrantType())
            throw new NotImplementedException("The specified grant type is not implemented.");

        if (context.Request.Username != config.Username || context.Request.Password != config.Password)
        {
            context.Reject(OpenIddictConstants.Errors.InvalidGrant, "The username or password is incorrect.");
            return ValueTask.CompletedTask;
        }

        var identity = new ClaimsIdentity(context.Request.GrantType);
        identity.AddClaim(OpenIddictConstants.Claims.Subject, config.Username);
        identity.AddClaim(OpenIddictConstants.Claims.Name, config.Username, OpenIddictConstants.Destinations.AccessToken);

        context.SignIn(new ClaimsPrincipal(identity));
        return ValueTask.CompletedTask;
    }
}
