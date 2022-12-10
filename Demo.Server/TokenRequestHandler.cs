using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;
using static OpenIddict.Server.OpenIddictServerEvents;

namespace Demo.Server;

internal class TokenRequestHandler : IOpenIddictServerHandler<HandleTokenRequestContext>
{
    private readonly UserManager<IdentityUser> userManager;

    private readonly SignInManager<IdentityUser> signInManager;

    private readonly Config config;

    public TokenRequestHandler(IOptions<Config> config, UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
    {
        this.userManager = userManager;
        this.signInManager = signInManager;
        this.config = config.Value;
    }

    public async ValueTask HandleAsync(HandleTokenRequestContext context)
    {
        if (context.Request.IsAuthorizationCodeGrantType() || context.Request.IsRefreshTokenGrantType())
        {
            // Retrieve the user profile corresponding to the authorization code/refresh token.
            var user = await userManager.FindByIdAsync(context.Principal?.GetClaim(Claims.Subject));
            if (user is null)
            {
                context.Reject(Errors.InvalidGrant, "The token is no longer valid.");
                return;
            }

            // Ensure the user is still allowed to sign in.
            if (!await signInManager.CanSignInAsync(user))
            {
                context.Reject(Errors.InvalidGrant, "The user is no longer allowed to sign in.");
                return;
            }

            var identity = new ClaimsPrincipal(new ClaimsIdentity(context.Principal?.Claims,
                authenticationType: TokenValidationParameters.DefaultAuthenticationType,
                nameType: Claims.Name,
                roleType: Claims.Role));

            // Override the user claims present in the principal in case they
            // changed since the authorization code/refresh token was issued.
            identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
                    /*.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray())*/;

            identity.SetDestinations(identity.GetDestinations());

            // Returning a SignInResult will ask OpenIddict to issue the appropriate access/identity tokens.
            context.SignIn(identity);
            return;
        }

        if (context.Request.IsPasswordGrantType())
        {
            if (context.Request.Username != config.Username || context.Request.Password != config.Password)
            {
                context.Reject(Errors.InvalidGrant, "The username or password is incorrect.");
                return;
            }

            var identity = new ClaimsIdentity(context.Request.GrantType);
            identity.AddClaim(Claims.Subject, config.Username);
            identity.AddClaim(Claims.Name, config.Username, Destinations.AccessToken);

            context.SignIn(new ClaimsPrincipal(identity));
            return;
        }

        throw new NotImplementedException("The specified grant type is not implemented.");
    }
}