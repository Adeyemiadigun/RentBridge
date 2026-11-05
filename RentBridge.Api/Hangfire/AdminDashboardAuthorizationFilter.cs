using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Hangfire.Dashboard;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using RentBridge.Api.Security;
using RentBridge.Infrastructure.Services;

namespace RentBridge.Api.Hangfire;

/// <summary>
/// Hangfire dashboard access control:
/// - Development: open to any local user (jobs and data can be inspected freely).
/// - Production: only requests carrying an Admin-role JWT may view the dashboard.
///
/// Production sources for the token, in order:
/// 1. The standard Authorization: Bearer header — API tools (Postman/curl)
///    work out of the box because UseAuthentication already populated
///    HttpContext.User before this filter runs.
/// 2. "rb_hangfire_admin" cookie — browser flow. AuthController sets this
///    HttpOnly, /hangfire-scoped cookie on every successful admin
///    login/refresh so admins can open the dashboard in a browser.
///
/// The cookie is only ever *read* here; it grants nothing unless the JWT
/// inside it validates (same rules as the API) AND its Role claim is Admin.
/// </summary>
public sealed class AdminDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    private const string AdminRole = "Admin";

    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        if (httpContext is null)
        {
            return false;
        }

        var environment = httpContext.RequestServices.GetService<IHostEnvironment>();
        if (environment?.IsDevelopment() ?? false)
        {
            return true;
        }

        // Path 1: header-authenticated principal (populated by UseAuthentication).
        if (httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole(AdminRole))
        {
            return true;
        }

        // Path 2: dashboard cookie issued by AuthController (browser flow).
        var token = httpContext.Request.Cookies[HangfireDashboardCookie.Name];
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var jwtOptions = httpContext.RequestServices
            .GetRequiredService<IOptions<JwtOptions>>()
            .Value;
        return IsAdminToken(token, jwtOptions);
    }

    /// <summary>
    /// Validates a raw JWT against the same issuer/audience/lifetime/key rules
    /// as the API, then checks the Role claim. Any validation failure fails
    /// closed (denied access).
    /// </summary>
    private static bool IsAdminToken(string token, JwtOptions jwtOptions)
    {
        try
        {
            var principal = new JwtSecurityTokenHandler().ValidateToken(
                token,
                TokenValidationParametersFactory.Create(jwtOptions),
                out _);

            return principal.Identity?.IsAuthenticated == true
                && principal.IsInRole(AdminRole);
        }
        // SecurityTokenException covers expired/invalid signatures/wrong issuer,
        // ArgumentException covers structurally malformed tokens.
        catch (Exception ex) when (ex is SecurityTokenException or ArgumentException)
        {
            return false;
        }
    }
}
