using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Admin;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Application.Query.Admin;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Admin-only administration endpoints. Authorization is enforced at the
/// handler level (current-user role checks) like the rest of the API.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/admin")]
public sealed class AdminController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Verifies a pending lawyer so they become auto-assignable to property
    /// reviews and lease legal review.
    /// </summary>
    [HttpPost("users/{userId:guid}/verify-lawyer")]
    public async Task<IActionResult> VerifyLawyer(Guid userId, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyLawyerCommand(userId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { userId = result.Value, verified = true });
    }

    /// <summary>
    /// Suspends a lawyer so they are no longer auto-assignable to property
    /// reviews and lease legal review. A suspended lawyer can be re-verified.
    /// </summary>
    [HttpPost("users/{userId:guid}/suspend-lawyer")]
    public async Task<IActionResult> SuspendLawyer(Guid userId, CancellationToken ct)
    {
        var result = await mediator.Send(new SuspendLawyerCommand(userId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { userId = result.Value, suspended = true });
    }

    /// <summary>
    /// Rejects a pending lawyer application. Rejection is terminal.
    /// </summary>
    [HttpPost("users/{userId:guid}/reject-lawyer")]
    public async Task<IActionResult> RejectLawyer(Guid userId, CancellationToken ct)
    {
        var result = await mediator.Send(new RejectLawyerCommand(userId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { userId = result.Value, rejected = true });
    }

    /// <summary>
    /// Returns the current global escrow fee-split percentages
    /// (platform commission + lawyer's legal-fee share).
    /// </summary>
    [HttpGet("settings/fees")]
    public async Task<IActionResult> GetFees(CancellationToken ct)
    {
        var result = await mediator.Send(new GetFeeSettingsQuery(), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Updates the global escrow fee-split percentages. Both rates are
    /// non-negative, below 100, and their sum stays below 100.
    /// </summary>
    [HttpPut("settings/fees")]
    public async Task<IActionResult> UpdateFees([FromBody] UpdateFeeSettingsCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}