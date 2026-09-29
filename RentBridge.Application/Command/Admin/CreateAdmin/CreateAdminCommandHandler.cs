using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Application.Common.Options;
using RentBridge.Domain.Aggregates;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Application.Command.Admin;

public sealed class CreateAdminCommandHandler(
    ILogger<CreateAdminCommandHandler> logger,
    IUnitOfWork unitOfWork,
    ICurrentUser currentUser,
    IPasswordService passwordService,
    IOptions<AdminOptions> adminOptions)
    : IRequestHandler<CreateAdminCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateAdminCommand request, CancellationToken cancellationToken)
    {
        // Only the seeded default admin (Admin:Email) can create admin accounts.
        if (currentUser.UserId is not Guid callerId)
        {
            return Result<Guid>.Fail("Authentication required");
        }

        var caller = await unitOfWork.Repository<User>().GetByIdAsync(callerId, cancellationToken);
        var defaultEmail = adminOptions.Value.Email;
        if (caller is null
            || caller.Role != UserRole.Admin
            || string.IsNullOrWhiteSpace(defaultEmail)
            || !string.Equals(caller.Email.Value, defaultEmail, StringComparison.OrdinalIgnoreCase))
        {
            logger.LogInformation("Blocked non-default admin creation attempt by user {UserId}", callerId);
            return Result<Guid>.Fail("Only the default admin can create admin accounts.");
        }

        var emailExists = await unitOfWork.Repository<User>()
            .AnyAsync(x => x.Email.Value == request.Email, cancellationToken);
        if (emailExists)
        {
            return Result<Guid>.Fail("Email already exists");
        }

        var phoneExists = await unitOfWork.Repository<User>()
            .AnyAsync(x => x.Phone.Value == request.Phone, cancellationToken);
        if (phoneExists)
        {
            return Result<Guid>.Fail("Phone number already exists");
        }

        var resEmail = Email.Create(request.Email);
        if (resEmail.IsSuccess is false)
        {
            return Result<Guid>.Fail(resEmail.Error);
        }

        var resPhone = PhoneNumber.Create(request.Phone);
        if (resPhone.IsSuccess is false)
        {
            return Result<Guid>.Fail(resPhone.Error);
        }

        var (passwordHash, salt) = passwordService.Generate(request.Password);

        var admin = new User(resEmail.Value, resPhone.Value, request.FirstName, request.LastName, UserRole.Admin);
        admin.SetPassword(passwordHash, salt);
        unitOfWork.Repository<User>().Add(admin);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Admin {AdminId} created by default admin {CallerId}", admin.Id, callerId);
        return Result<Guid>.Ok(admin.Id);
    }
}
