using System.Security.Claims;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Primitives;
using Microsoft.IdentityModel.Tokens;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace Demo.Server.Controllers;

[Route("/oauth/[action]")]
public class OAuthController : Controller
{
    private readonly IOpenIddictApplicationManager applicationManager;

    private readonly IOpenIddictAuthorizationManager authorizationManager;

    private readonly IOpenIddictScopeManager scopeManager;

    private readonly UserManager<IdentityUser> userManager;

    public OAuthController(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictAuthorizationManager authorizationManager,
        IOpenIddictScopeManager scopeManager,
        UserManager<IdentityUser> userManager)
    {
        this.applicationManager = applicationManager;
        this.authorizationManager = authorizationManager;
        this.scopeManager = scopeManager;
        this.userManager = userManager;
    }

    [HttpPost, FormValueRequired("submit.Accept"), ActionName("Authorize"),]
    public async Task<IActionResult> Accept()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException();
        var user = await userManager.GetUserAsync(User) ??
                   throw new InvalidOperationException("The user details cannot be retrieved.");
        var application = await applicationManager.FindByClientIdAsync(request.ClientId ?? throw new InvalidOperationException(), HttpContext.RequestAborted) ??
                          throw new InvalidOperationException();

        var authorizations = await authorizationManager.FindAsync(
            await userManager.GetUserIdAsync(user),
            await applicationManager.GetIdAsync(application) ?? string.Empty,
            Statuses.Valid,
            AuthorizationTypes.Permanent,
            request.GetScopes()).ToListAsync();

        var identity = new ClaimsPrincipal(new ClaimsIdentity(
            TokenValidationParameters.DefaultAuthenticationType,
            Claims.Name,
            Claims.Role));

        // Add the claims that will be persisted in the tokens.
        identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
            .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
            .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
            /*.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray())*/;

        // Note: in this sample, the granted scopes match the requested scope
        // but you may want to allow the user to uncheck specific scopes.
        // For that, simply restrict the list of scopes before calling SetScopes.
        identity.SetScopes(request.GetScopes());
        identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

        // Automatically create a permanent authorization to avoid requiring explicit consent
        // for future authorization or token requests containing the same scopes.
        var authorization = authorizations.LastOrDefault();
        authorization ??= await authorizationManager.CreateAsync(
            identity,
            await userManager.GetUserIdAsync(user),
            await applicationManager.GetIdAsync(application) ?? throw new InvalidOperationException(),
            AuthorizationTypes.Permanent,
            identity.GetScopes());

        identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
        identity.SetDestinations(identity.GetDestinations());

        return SignIn(identity, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
    }

    [HttpGet, HttpPost,]
    public async Task<IActionResult> Authorize()
    {
        var request = HttpContext.GetOpenIddictServerRequest() ?? throw new InvalidOperationException();
        var result = await HttpContext.AuthenticateAsync();

        if (!result.Succeeded || request.HasPrompt(Prompts.Login))
        {
            if (request.HasPrompt(Prompts.None))
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.LoginRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = "The user is not logged in.",
                    }),
                    OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);

            var prompt = string.Join(" ", request.GetPrompts().Remove(Prompts.Login));

            var parameters = HttpContext.Request.HasFormContentType ? HttpContext.Request.Form.Where(parameter => parameter.Key != Parameters.Prompt).ToList() : HttpContext.Request.Query.Where(parameter => parameter.Key != Parameters.Prompt).ToList();

            parameters.Add(KeyValuePair.Create(Parameters.Prompt, new StringValues(prompt)));

            return Challenge(
                new AuthenticationProperties
                {
                    RedirectUri = HttpContext.Request.PathBase + HttpContext.Request.Path + QueryString.Create(parameters),
                },
                IdentityConstants.ApplicationScheme);
        }

        var user = await userManager.GetUserAsync(result.Principal) ??
                   throw new InvalidOperationException("The user details cannot be retrieved.");
        var application = await applicationManager.FindByClientIdAsync(request.ClientId ?? throw new InvalidOperationException(), HttpContext.RequestAborted) ??
                          throw new InvalidOperationException();

        var authorizations = await authorizationManager.FindAsync(
            await userManager.GetUserIdAsync(user),
            await applicationManager.GetIdAsync(application) ?? string.Empty,
            Statuses.Valid,
            AuthorizationTypes.Permanent,
            request.GetScopes()).ToListAsync();

        var consentType = await applicationManager.GetConsentTypeAsync(application);
        switch (consentType)
        {
            case ConsentTypes.External when !authorizations.Any():
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "The logged in user is not allowed to access this client application.",
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            case ConsentTypes.Implicit:
            case ConsentTypes.External when authorizations.Any():
            case ConsentTypes.Explicit when authorizations.Any() && !request.HasPrompt(Prompts.Consent):
                // Create the claims-based identity that will be used by OpenIddict to generate tokens.
                var identity = new ClaimsPrincipal(new ClaimsIdentity(
                    TokenValidationParameters.DefaultAuthenticationType,
                    Claims.Name,
                    Claims.Role));

                // Add the claims that will be persisted in the tokens.
                identity.SetClaim(Claims.Subject, await userManager.GetUserIdAsync(user))
                    .SetClaim(Claims.Email, await userManager.GetEmailAsync(user))
                    .SetClaim(Claims.Name, await userManager.GetUserNameAsync(user))
                    /*.SetClaims(Claims.Role, (await userManager.GetRolesAsync(user)).ToImmutableArray())*/;

                // Note: in this sample, the granted scopes match the requested scope
                // but you may want to allow the user to uncheck specific scopes.
                // For that, simply restrict the list of scopes before calling SetScopes.
                identity.SetScopes(request.GetScopes());
                identity.SetResources(await scopeManager.ListResourcesAsync(identity.GetScopes()).ToListAsync());

                // Automatically create a permanent authorization to avoid requiring explicit consent
                // for future authorization or token requests containing the same scopes.
                var authorization = authorizations.LastOrDefault();
                authorization ??= await authorizationManager.CreateAsync(
                    identity,
                    await userManager.GetUserIdAsync(user),
                    await applicationManager.GetIdAsync(application) ?? throw new InvalidOperationException(),
                    AuthorizationTypes.Permanent,
                    identity.GetScopes());

                identity.SetAuthorizationId(await authorizationManager.GetIdAsync(authorization));
                identity.SetDestinations(identity.GetDestinations());

                return SignIn(identity, OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            case ConsentTypes.Explicit when request.HasPrompt(Prompts.None):
            case ConsentTypes.Systematic when request.HasPrompt(Prompts.None):
                return Forbid(
                    new AuthenticationProperties(new Dictionary<string, string?>
                    {
                        [OpenIddictServerAspNetCoreConstants.Properties.Error] = Errors.ConsentRequired,
                        [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] =
                            "Interactive user consent is required.",
                    }),
                    OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);

            default:
                return Content(@$"
<p class=""lead text-left"">Do you want to grant <strong>{applicationManager.GetLocalizedDisplayNameAsync(application)}</strong> access to your data? (scopes requested: {request.Scope})</p>
<form action=""/oauth/authorize"" method=""post"">
{string.Join("\n", (HttpContext.Request.HasFormContentType ? (IEnumerable<KeyValuePair<string, StringValues>>) HttpContext.Request.Form : HttpContext.Request.Query).Select(p => $"<input type=\"hidden\" name=\"{p.Key}\" value=\"{p.Value}\" />"))}
<input class=""btn btn-lg btn-success"" name=""submit.Accept"" type=""submit"" value=""Yes"" />
<input class=""btn btn-lg btn-danger"" name=""submit.Deny"" type=""submit"" value=""No"" />
</form>
", "text/html");
        }
    }
}
