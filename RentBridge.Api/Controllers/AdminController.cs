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
[Route("api/admin")]
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