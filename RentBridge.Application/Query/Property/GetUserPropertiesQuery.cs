using MediatR;
using RentBridge.Application.Common;
using RentBridge.Application.Dtos.Properties;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Property;

public sealed record GetUserPropertiesQuery(
    int Page = 1,
    int PageSize = 20,
    bool? IsVerified = null,
    string? State = null,
    string? City = null) : IRequest<Result<PagedResult<PropertyItem>>>;