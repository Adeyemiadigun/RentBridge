using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Auth
{
    public record LogoutCommand : IRequest<Result>
    {
        public string RefreshToken { get; init; } = string.Empty;
    }
}
