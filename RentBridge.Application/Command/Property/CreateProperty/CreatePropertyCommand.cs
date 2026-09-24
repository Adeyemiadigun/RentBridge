using MediatR;
using RentBridge.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.Property
{
    

    public sealed record CreatePropertyCommand(
        string Street,
        string City,
        string Area,
        string State,
        List<string> DocumentUrls,
        string? PropertyType = null,
        int Bedrooms = 0,
        int Bathrooms = 0,
        string? AvailableFrom = null,
        List<string>? Amenities = null) : IRequest<Result<Guid>>;
}
