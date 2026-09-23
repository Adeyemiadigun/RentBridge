using MediatR;
using RentBridge.Domain.Common;

namespace RentBridge.Application.Command.Admin;

/// <summary>
/// Creates a new admin account. Only the seeded default admin
/// (Admin:Email) may do this — enforced in the handler.
/// </summary>
public sealed record CreateAdminCommand(
    string Email,
    string Phone,
    string FirstName,
    string LastName,
    string Password
) : IRequest<Result<Guid>>;
