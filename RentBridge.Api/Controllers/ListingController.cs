using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RentBridge.Application.Command.Listing;
using RentBridge.Application.Query.Listing;

namespace RentBridge.Api.Controllers;

[ApiController]
[Route("api/listings")]
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