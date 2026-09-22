using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Admin;
using RentBridge.Application.Dtos.Admin;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Application.Query.Admin;
using RentBridge.Domain.Enums;

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
    /// Platform-wide operational view: user/listing/lease counts, money held
    /// in escrow, and transaction metrics from the ledger.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(CancellationToken ct)
    {
        var result = await mediator.Send(new GetAdminDashboardQuery(), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Platform-wide transaction time-series for charts, grouped in SQL by
    /// UTC day, Monday-start week, or calendar month. Empty buckets are
    /// returned as zeros; `to` is exclusive.
    /// </summary>
    [HttpGet("metrics/transactions")]
    public async Task<IActionResult> GetTransactionMetrics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] MetricsGranularity granularity = MetricsGranularity.Day,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetTransactionMetricsQuery(from, to, granularity), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Lawyer panel: paged lawyers for the approve/reject/suspend workflow.
    /// Filter by status (e.g. Pending for the verification queue).
    /// </summary>
    [HttpGet("lawyers")]
    public async Task<IActionResult> GetLawyers(
        [FromQuery] LawyerStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetLawyersQuery(status, page, pageSize), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Listing moderation queue: paged listings with owner and property
    /// verification state. Filter by status if needed.
    /// </summary>
    [HttpGet("listings")]
    public async Task<IActionResult> GetListingsForModeration(
        [FromQuery] ListingStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetListingsForModerationQuery(status, page, pageSize), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
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