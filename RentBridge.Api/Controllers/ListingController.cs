using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Api.Dtos;
using RentBridge.Application.Command.Listing;
using RentBridge.Application.Query.Listing;

namespace RentBridge.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/listings")]
public sealed class ListingController(IMediator mediator) : ControllerBase
{
    /// <summary>
    /// Creates a listing (Draft) from a verified property owned by the
    /// current user. The listing must be published before tenants can see it.
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<IActionResult> Create([FromBody] ListPropertyCommand command, CancellationToken ct)
    {
        var result = await mediator.Send(command, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { listingId = result.Value });
    }

    /// <summary>
    /// Publishes a Draft listing. The owner must be identity-verified and
    /// the property must be ownership-verified. Raises ListingPublished
    /// (emails the owner).
    /// </summary>
    [HttpPost("{listingId:guid}/publish")]
    [Authorize]
    public async Task<IActionResult> Publish(Guid listingId, CancellationToken ct)
    {
        var result = await mediator.Send(new PublishListingCommand(listingId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { listingId = result.Value });
    }

    /// <summary>
    /// Edits the owner's listing (title, description, and/or price — only
    /// the fields sent are changed). Closed listings cannot be edited.
    /// </summary>
    [HttpPatch("{listingId:guid}")]
    [Authorize]
    public async Task<IActionResult> Edit(Guid listingId, [FromBody] EditListingRequest request, CancellationToken ct)
    {
        var result = await mediator.Send(
            new EditListingCommand(listingId, request.Title, request.Description, request.PriceAmount), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { listingId = result.Value });
    }

    /// <summary>
    /// Takes a published listing off public search. It can be published again.
    /// </summary>
    [HttpPost("{listingId:guid}/unpublish")]
    [Authorize]
    public async Task<IActionResult> Unpublish(Guid listingId, CancellationToken ct)
    {
        var result = await mediator.Send(new UnpublishListingCommand(listingId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { listingId = result.Value });
    }

    /// <summary>
    /// Closes a listing permanently. Closed listings leave public search and
    /// cannot be edited or re-published.
    /// </summary>
    [HttpPost("{listingId:guid}/close")]
    [Authorize]
    public async Task<IActionResult> Close(Guid listingId, CancellationToken ct)
    {
        var result = await mediator.Send(new CloseListingCommand(listingId), ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(new { listingId = result.Value });
    }

    /// <summary>
    /// Public listing search with DB-level pagination and filters.
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<IActionResult> Search([FromQuery] SearchListingsQuery query, CancellationToken ct)
    {
        var result = await mediator.Send(query, ct);
        if (result.IsSuccess is false)
        {
            return BadRequest(new { error = result.Error });
        }

        return Ok(result.Value);
    }
}