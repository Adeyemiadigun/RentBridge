using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Api.Dtos;
using RentBridge.Application.Command.PayoutAccount;
using RentBridge.Application.Query.PayoutAccount;

namespace RentBridge.Api.Controllers;

/// <summary>
/// Bank-account management for escrow payouts. A landlord, agent, or caretaker
/// registers one verified payout account here; it is reused for every payout
/// they are entitled to. Publishing a listing is blocked until this exists.
/// </summary>
[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/payout-account")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class PayoutAccountController(IMediator mediator) : ControllerBase
{
    /// <summary>Lists the banks available for payout.</summary>
    [HttpGet("banks")]
    public async Task<IActionResult> GetBanks(CancellationToken ct)
    {
        var result = await mediator.Send(new GetBanksQuery(), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Name enquiry: resolves the account holder's name for a bank + account
    /// number. Use this to confirm before saving. Nothing is persisted.
    /// </summary>
    [HttpPost("resolve")]
    public async Task<IActionResult> Resolve([FromBody] ResolvePayoutAccountRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new ResolvePayoutAccountCommand(request.BankCode, request.AccountNumber), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { accountName = result.Value });
    }

    /// <summary>
    /// Registers (or replaces) the caller's escrow payout account. The account
    /// name is verified by the provider and a transfer recipient is created.
    /// </summary>
    [HttpPut]
    public async Task<IActionResult> Set([FromBody] SetPayoutAccountRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new SetPayoutAccountCommand(request.BankCode, request.BankName, request.AccountNumber), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>Returns the caller's registered payout account (masked), if any.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var result = await mediator.Send(new GetPayoutAccountQuery(), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}
