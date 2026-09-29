using MediatR;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Query.Auth;

public sealed record GetMyProfileQuery : IRequest<Result<UserProfileResponse>>;