using System.Linq;
using System.Threading.Tasks;
using System.Web;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SecurityTokenService.Extensions;

namespace SecurityTokenService.Controllers
{
    [SecurityHeaders]
    [Route("[controller]")]
    [Authorize]
    public class ConsentController : ControllerBase
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IResourceStore _resourceStore;
        private readonly IEventService _events;
        private readonly ILogger<ConsentController> _logger;

        public ConsentController(IClientStore clientStore, IIdentityServerInteractionService interaction,
            IResourceStore resourceStore, IEventService events, ILogger<ConsentController> logger)
        {
            _clientStore = clientStore;
            _interaction = interaction;
            _resourceStore = resourceStore;
            _events = events;
            _logger = logger;
        }

        /// <summary>
        /// Shows the consent screen
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Index(string returnUrl)
        {
            var request = await _interaction.GetAuthorizationContextAsync(returnUrl);
            string error;
            if (request != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(request.Client.ClientId);
                if (client != null)
                {
                    var resources = await _resourceStore.FindEnabledResourcesByScopeAsync(request.Client.AllowedScopes);
                    if (resources != null && (resources.IdentityResources.Any() || resources.ApiResources.Any()))
                    {
                        var output = await CreateConsentOutputAsync(returnUrl, client, resources);
                        return new ObjectResult(new ApiResult
                        {
                            Data = output
                        });
                    }
                    else
                    {
                        error = $"No scopes matching: {request.Client.AllowedScopes.Aggregate((x, y) => x + ", " + y)}";
                        _logger.LogError(error);
                        return new ObjectResult(new ApiResult
                        {
                            Message = error,
                            Code = Errors.ConsentNoScopesMatching
                        });
                    }
                }
                else
                {
                    error = $"Invalid client id: {request.Client.ClientId}";
                    _logger.LogError(error);
                    return new ObjectResult(new ApiResult
                    {
                        Message = error,
                        Code = Errors.InvalidClientId
                    });
                }
            }
            else
            {
                error = $"No consent request matching request: {returnUrl}";
                _logger.LogError(error);
                return new ObjectResult(new ApiResult
                {
                    Message = error,
                    Code = Errors.NoConsentRequestMatchingRequest
                });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Index(Inputs.V1.ConsentInput model)
        {
            // validate return url is still valid
            var request = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            ConsentResponse grantedConsent;

            // user clicked 'no' - send back the standard 'access_denied' response
            if (model.Button == "no")
            {
                grantedConsent = new ConsentResponse { Error = AuthorizationError.AccessDenied };

                // emit event
                await _events.RaiseAsync(new ConsentDeniedEvent(User.GetSubjectId(), request.Client.ClientId,
                    request.ValidatedResources.RawScopeValues));
            }
            // user clicked 'yes' - validate the data
            else if (model.Button == "yes")
            {
                // if the user consented to some scope, build the response model
                if (model.ScopesConsented != null && model.ScopesConsented.Any())
                {
                    var scopes = model.ScopesConsented;
                    if (ConsentOptions.EnableOfflineAccess == false)
                    {
                        scopes = scopes.Where(x =>
                            x != IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess);
                    }

                    grantedConsent = new ConsentResponse
                    {
                        RememberConsent = model.RememberConsent == "on",
                        ScopesValuesConsented = scopes.ToArray(),
                        Description = model.Description
                    };

                    // emit event
                    await _events.RaiseAsync(new ConsentGrantedEvent(User.GetSubjectId(), request.Client.ClientId,
                        request.ValidatedResources.RawScopeValues, grantedConsent.ScopesValuesConsented,
                        grantedConsent.RememberConsent));
                }
                else
                {
                    // result.ValidationError = ConsentOptions.MustChooseOneErrorMessage;
                    return Redirect("error.html?errorId=");
                }
            }
            else
            {
                return Redirect("~/error.html?errorId=" + Errors.ConsentInvalidSelection);
            }

            // communicate outcome of consent back to identityserver
            await _interaction.GrantConsentAsync(request, grantedConsent);

            // indicate that's it ok to redirect back to authorization endpoint
            var redirectUrl = model.ReturnUrl;

            if (!string.IsNullOrWhiteSpace(redirectUrl))
            {
                if (await _clientStore.IsPkceClientAsync(request.Client.ClientId))
                {
                    // if the client is PKCE then we assume it's native, so this change in how to
                    // return the response is for better UX for the end user.
                    return Redirect("~/redirect.html?redirectUrl=" + HttpUtility.UrlEncode(redirectUrl));
                }

                return Redirect(redirectUrl);
            }

            // 重新询问
            return Redirect(HttpContext.Request.Headers["Referer"]);
        }

        private async Task<Outputs.V1.ConsentOutput> CreateConsentOutputAsync(string returnUrl,
            Client client, Resources resources)
        {
            var vm = new Outputs.V1.ConsentOutput
            {
                ReturnUrl = returnUrl,
                ClientName = client.ClientName ?? client.ClientId,
                ClientUrl = client.ClientUri,
                ClientLogoUrl = client.LogoUri,
                AllowRememberConsent = client.AllowRememberConsent,
                IdentityScopes = resources.IdentityResources
                    .Select(x => CreateIdentityResourceScopeOutput(x, true)).ToArray()
            };

            var scopeNames = resources.ApiResources.SelectMany(x => x.Scopes).ToHashSet();
            var apiScopes = await _resourceStore.FindApiScopesByNameAsync(scopeNames);
            vm.ResourceScopes = apiScopes.Select(x =>
                CreateApiScopeOutput(x, true)).ToArray();
            if (ConsentOptions.EnableOfflineAccess && resources.OfflineAccess)
            {
                vm.ResourceScopes = vm.ResourceScopes.Union(new[]
                {
                    CreateScopeOutput(true)
                });
            }

            return vm;
        }

        private Outputs.V1.ScopeOutput CreateIdentityResourceScopeOutput(IdentityResource identity, bool check)
        {
            return new Outputs.V1.ScopeOutput
            {
                Name = identity.Name,
                DisplayName = identity.DisplayName,
                Description = identity.Description,
                Emphasize = identity.Emphasize,
                Required = identity.Required,
                Checked = check || identity.Required
            };
        }

        public Outputs.V1.ScopeOutput CreateApiScopeOutput(ApiScope scope, bool check)
        {
            return new Outputs.V1.ScopeOutput
            {
                Name = scope.Name,
                DisplayName = scope.DisplayName,
                Description = scope.Description,
                Emphasize = scope.Emphasize,
                Required = scope.Required,
                Checked = check || scope.Required
            };
        }

        private Outputs.V1.ScopeOutput CreateScopeOutput(bool check)
        {
            return new Outputs.V1.ScopeOutput
            {
                Name = IdentityServer4.IdentityServerConstants.StandardScopes.OfflineAccess,
                DisplayName = ConsentOptions.OfflineAccessDisplayName,
                Description = ConsentOptions.OfflineAccessDescription,
                Emphasize = true,
                Checked = check
            };
        }
    }
}