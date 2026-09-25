using MediatR;
using RentBridge.Application.Dtos.Properties;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Property;

/// <summary>
/// Lists properties with their ownership documents for manual review:
/// admins see every property, lawyers see only the ones assigned to them.
/// </summary>
public sealed record GetPropertyReviewsQuery : IRequest<Result<IReadOnlyList<PropertyReviewItem>>>;