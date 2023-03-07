// using System;
// using System.Threading.Tasks;
// using IdentityServer4.ResponseHandling;
// using IdentityServer4.Services;
// using IdentityServer4.Stores;
// using IdentityServer4.Validation;
// using Microsoft.AspNetCore.Authentication;
// using Microsoft.Extensions.Logging;
//
// namespace SecurityTokenService.IdentityServer;
//
// public class TokenResponseGenerator : IdentityServer4.ResponseHandling.TokenResponseGenerator
// {
//     public TokenResponseGenerator(ISystemClock clock, ITokenService tokenService,
//         IRefreshTokenService refreshTokenService, IScopeParser scopeParser, IResourceStore resources,
//         IClientStore clients, ILogger<IdentityServer4.ResponseHandling.TokenResponseGenerator> logger) : base(clock,
//         tokenService, refreshTokenService, scopeParser, resources, clients, logger)
//     {
//     }
//
//     public override async Task<TokenResponse> ProcessAsync(TokenRequestValidationResult request)
//     {
//         var response = await base.ProcessAsync(request);
//         response.Custom.Add("TokenTraceId", Guid.NewGuid().ToString("N"));
//         return response;
//     }
// }
