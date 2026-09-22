namespace RentBridge.Api.Dtos;

public sealed record EditListingRequest(string? Title = null, string? Description = null, decimal? PriceAmount = null);
