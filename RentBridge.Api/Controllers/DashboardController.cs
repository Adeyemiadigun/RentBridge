using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Dtos.Dashboard;
using RentBridge.Application.Query.Dashboard;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Operational dashboard for listing owners (landlord, agent, or caretaker):
/// their listings and leases, escrow held on their leases, payout totals,
/// and recent ledger lines.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns the signed-in owner's dashboard.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetOwnerDashboardQuery(), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Transaction time-series for the signed-in owner's leases, grouped in
    /// SQL by UTC day, Monday-start week, or calendar month. Empty buckets
    /// are returned as zeros; `to` is exclusive.
    /// </summary>
    [HttpGet("metrics/transactions")]
    public async Task<IActionResult> GetTransactionMetrics(
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] MetricsGranularity granularity = MetricsGranularity.Day,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetOwnerTransactionMetricsQuery(from, to, granularity), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
