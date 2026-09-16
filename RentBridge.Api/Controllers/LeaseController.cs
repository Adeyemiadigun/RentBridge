using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Api.Dtos;
using RentBridge.Application.Command.Lease;
using RentBridge.Application.Query.Lease;

namespace RentBridge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/leases")]
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