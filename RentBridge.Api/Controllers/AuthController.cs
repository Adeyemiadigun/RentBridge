using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using RentBridge.Application.Command.Auth;
using RentBridge.Application.Command.RegisterUser;
using RentBridge.Application.Query.Auth;
using RentBridge.Api.Hangfire;
using RentBridge.Infrastructure.Services;

namespace RentBridge.Api.Controllers;

[AllowAnonymous]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public sealed class AuthController(
    IMediator mediator,
    IOptions<JwtOptions> jwtOptions) : ControllerBase
{
    /// <summary>
    /// Registers a new user. Pass the role as one of: Landlord, Tenant,
    /// Caretaker, Agent, Lawyer. Lawyers must include a BarNumber.
    /// Admin accounts cannot be created here (seeded + default-admin only).
    /// </summary>
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] registerUserCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return result.ToErrorResponse();
        }

        return Ok(new { userId = result.Value });
    }

    [HttpPost("login")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        if (result.IsSuccess is false)
        {
            return Unauthorized(new { error = result.Error });
        }

        // Browser flow for the Hangfire dashboard: the cookie carries the access
        // JWT scoped to /hangfire only, so it is never attached to API calls.
        // The dashboard filter additionally requires an Admin role claim, so
        // this cookie is inert for non-admins.
        SetDashboardCookie(result.Value.Token);
        return Ok(result.Value);
    }

    [HttpPost("refresh")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshAccessTokenCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        if (result.IsSuccess is false)
        {
            return Unauthorized(new { error = result.Error });
        }

        // Keep an admin's dashboard session alive across token refreshes.
        SetDashboardCookie(result.Value.Token);
        return Ok(result.Value);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutCommand request, CancellationToken ct)
    {
        var result = await mediator.Send(request, ct);
        if (result.IsSuccess is false)
        {
            return result.ToErrorResponse();
        }

        // Drop the dashboard cookie if present; the JWT itself remains valid
        // until expiry (standard stateless-jwt trade-off), so the cookie
        // expiry keeps the blast radius short.
        Response.Cookies.Delete(
            HangfireDashboardCookie.Name,
            new CookieOptions { Path = "/hangfire", SameSite = SameSiteMode.Lax });
        return Ok(new { success = true });
    }

    /// <summary>
    /// Returns the caller's profile (name, email, phone, role, KYC status).
    /// The JWT only carries claims; this endpoint is the real profile source.
    /// </summary>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Me(CancellationToken ct)
    {
        var result = await mediator.Send(new GetMyProfileQuery(), ct);
        if (result.IsSuccess is false)
        {
            return result.ToErrorResponse();
        }

        return Ok(result.Value);
    }

    private void SetDashboardCookie(string token)
    {
        Response.Cookies.Append(HangfireDashboardCookie.Name, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            // Not Strict: admins clicking over from a different-origin admin
            // panel should still carry it on a top-level navigation.
            SameSite = SameSiteMode.Lax,
            // Scoped so the browser only sends it for the dashboard itself.
            Path = "/hangfire",
            // Track the access-token lifetime so the cookie disappears when
            // the token would fail validation anyway.
            MaxAge = TimeSpan.FromMinutes(jwtOptions.Value.ExpirationInMinutes),
            IsEssential = true,
        });
    }
}