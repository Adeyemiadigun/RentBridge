using MediatR;
using RentBridge.Application.Dtos.Auth;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Auth
{
    public record RefreshAccessTokenCommand : IRequest<Result<LoginResponse>>
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
