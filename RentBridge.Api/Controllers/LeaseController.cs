using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Api.Dtos;
using RentBridge.Application.Command.Lease;
using RentBridge.Application.Query.Lease;
using RentBridge.Application.Query.Transaction;

namespace RentBridge.Api.Controllers;

[Authorize]
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/leases")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public sealed class LeaseController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// A tenant initiates a lease on a published listing. The listing's
    /// owner becomes the landlord. Lease starts in Initiated state.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateLeaseCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { leaseId = result.Value });
    }

    /// <summary>
    /// Lists leases involving the caller (as landlord, tenant or assigned
    /// lawyer; admins see all), newest first, with party names and the
    /// latest inspection request included per row.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var result = await mediator.Send(new GetCallerLeasesQuery(page, pageSize), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// The tenant requests an inspection date. Adds a pending inspection
    /// request to the lease.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection")]
    public async Task<IActionResult> RequestInspection(Guid leaseId, [FromBody] RequestInspectionRequest request, CancellationToken ct)
    {
        var command = new RequestInspectionCommand(leaseId, request.PreferredDate, request.Note);
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The landlord begins the inspection flow (Initiated → InspectionRequested).
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/begin")]
    public async Task<IActionResult> BeginInspection(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new BeginInspectionFlowCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The landlord or an admin confirms the inspection
    /// (InspectionRequested → InspectionConfirmed). Optionally records the
    /// scheduled physical-inspection date and notes. Raises InspectionConfirmed.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/confirm")]
    public async Task<IActionResult> ConfirmInspection(Guid leaseId, [FromBody] ConfirmInspectionRequest? request, CancellationToken ct)
    {
        var command = new ConfirmInspectionCommand(leaseId, request?.ScheduledDate, request?.Notes);
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The landlord or an admin declines the inspection request
    /// (back to Initiated). Raises InspectionDeclined.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/decline")]
    public async Task<IActionResult> DeclineInspection(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new DeclineInspectionCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The tenant cancels their own pending inspection request
    /// (back to Initiated). Raises InspectionCancelled.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/cancel")]
    public async Task<IActionResult> CancelInspection(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new CancelInspectionCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The tenant proposes a new date for a confirmed inspection.
    /// Raises InspectionRescheduleRequested.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/reschedule")]
    public async Task<IActionResult> RequestReschedule(Guid leaseId, [FromBody] RequestInspectionRequest request, CancellationToken ct)
    {
        var command = new RequestRescheduleCommand(leaseId, request.PreferredDate, request.Note);
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The landlord or an admin accepts the reschedule request.
    /// Raises InspectionRescheduled.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/reschedule/confirm")]
    public async Task<IActionResult> ConfirmReschedule(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new ConfirmRescheduleCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The landlord or an admin rejects the reschedule request
    /// (original schedule is kept). Raises InspectionRescheduleRejected.
    /// </summary>
    [HttpPost("{leaseId:guid}/inspection/reschedule/reject")]
    public async Task<IActionResult> RejectReschedule(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new RejectRescheduleCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The tenant, landlord, lawyer, or an admin moves the lease into legal
    /// review. If no lawyer was auto-assigned at inspection-confirm time, one is
    /// picked lazily before the transition.
    /// </summary>
    [HttpPost("{leaseId:guid}/legal-review")]
    public async Task<IActionResult> BeginLegalReview(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new BeginLegalReviewCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The assigned lawyer certifies the agreement (LegalReview → Certified).
    /// Raises AgreementCertified.
    /// </summary>
    [HttpPost("{leaseId:guid}/certify")]
    public async Task<IActionResult> CertifyAgreement(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new CertifyAgreementCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// The tenant or landlord signs the agreement (Certified → PartiallySigned /
    /// FullySigned). Raises AgreementFullySigned once both parties have signed.
    /// </summary>
    [HttpPost("{leaseId:guid}/sign")]
    public async Task<IActionResult> SignAgreement(Guid leaseId, [FromBody] SignAgreementRequest? request, CancellationToken ct)
    {
        var command = new SignAgreementCommand(leaseId, request?.SignatureImage);
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Returns the canonical tenancy agreement content (terms + pinned hash +
    /// certification/signature status). Composes the draft lazily on first view.
    /// </summary>
    [HttpGet("{leaseId:guid}/agreement")]
    public async Task<IActionResult> GetAgreement(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLeaseAgreementQuery(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns the read-only rendered PDF of the agreement (terms + certified-by
    /// and signature blocks + audit trail).
    /// </summary>
    [HttpGet("{leaseId:guid}/agreement/pdf")]
    [Produces("application/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAgreementPdf(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLeaseAgreementPdfQuery(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        var pdf = result.Value;
        return File(pdf, "application/pdf", $"agreement-{leaseId}.pdf");
    }

    /// <summary>
    /// The tenant funds escrow for a fully-signed lease. Returns the provider
    /// checkout URL. Idempotent: re-calling returns the same checkout.
    /// </summary>
    [HttpPost("{leaseId:guid}/escrow/fund")]
    public async Task<IActionResult> FundEscrow(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new FundEscrowCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Admin-only fallback: forces or retries the automatic escrow payout
    /// (net of platform commission and the lawyer's legal-fee share). The
    /// verification gates are still enforced. Privileged and audit-logged.
    /// </summary>
    [HttpPost("{leaseId:guid}/escrow/release")]
    public async Task<IActionResult> ReleaseEscrow(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new ReleaseEscrowCommand(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { released = true });
    }

    /// <summary>
    /// Returns the escrow ledger lines for one lease, newest first
    /// (fund → fees → payout). Accessible to the landlord, tenant,
    /// assigned lawyer, or an admin.
    /// </summary>
    [HttpGet("{leaseId:guid}/transactions")]
    public async Task<IActionResult> GetTransactions(
        Guid leaseId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await mediator.Send(new GetLeaseTransactionsQuery(leaseId, page, pageSize), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Returns lease detail (status, agreement certification and signature
    /// state, escrow payment trail). Accessible to the landlord, tenant,
    /// assigned lawyer, or an admin.
    /// </summary>
    [HttpGet("{leaseId:guid}")]
    public async Task<IActionResult> Get(Guid leaseId, CancellationToken ct)
    {
        var result = await mediator.Send(new GetLeaseQuery(leaseId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}