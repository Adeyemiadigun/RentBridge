using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Property;
using RentBridge.Application.Query.Property;

namespace RentBridge.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/properties")]
public sealed class PropertyController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a property (Draft) and attaches the ownership documents.
    /// The landlord/caretaker/agent who creates it becomes the owner-actor.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreatePropertyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { propertyId = result.Value });
    }

    /// <summary>
    /// A lawyer/admin flags a property document for review.
    /// </summary>
    [HttpPost("{propertyId:guid}/documents/{documentId:guid}/start-review")]
    public async Task<IActionResult> StartDocumentReview(Guid propertyId, Guid documentId, CancellationToken ct)
    {
        var result = await mediator.Send(new StartDocumentReviewCommand(propertyId, documentId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// A lawyer/admin verifies a property document.
    /// </summary>
    [HttpPost("{propertyId:guid}/documents/{documentId:guid}/verify")]
    public async Task<IActionResult> VerifyDocument(Guid propertyId, Guid documentId, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyDocumentCommand(propertyId, documentId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// A lawyer/admin rejects a property document, optionally with a reason.
    /// </summary>
    [HttpPost("{propertyId:guid}/documents/{documentId:guid}/reject")]
    public async Task<IActionResult> RejectDocument(Guid propertyId, Guid documentId, [FromQuery] string? reason, CancellationToken ct)
    {
        var result = await mediator.Send(new RejectDocumentCommand(propertyId, documentId, reason), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// A lawyer/admin verifies property ownership. Raises OwnershipVerified,
    /// which emails the owner and unlocks listing for the property.
    /// </summary>
    [HttpPost("{propertyId:guid}/verify")]
    public async Task<IActionResult> VerifyProperty(Guid propertyId, CancellationToken ct)
    {
        var result = await mediator.Send(new VerifyPropertyCommand(propertyId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { success = true });
    }

    /// <summary>
    /// Lists the current user's properties (landlord/caretaker/agent)
    /// with pagination and optional filters.
    /// </summary>
    [HttpGet("mine")]
    public async Task<IActionResult> Mine([FromQuery] GetUserPropertiesQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}