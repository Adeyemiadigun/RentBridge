using MediatR;
using Microsoft.Extensions.Logging;
using RentBridge.Application.Common.Interfaces;
using RentBridge.Application.Common.Interfaces.Repositories;
using RentBridge.Domain.Aggregates.Users;
using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;
using System;
using System.Collections.Generic;
using System.Text;

namespace RentBridge.Application.Command.RegisterUser
{
    public class RegisterUserCommandHandler(ILogger<RegisterUserCommandHandler> logger, IUnitOfWork _unitOfWork, IPasswordService passwordService) : IRequestHandler<registerUserCommand, Result<Guid>>
    {
        public async Task<Result<Guid>> Handle(registerUserCommand request, CancellationToken cancellationToken)
        {
            
                var emailExists = await _unitOfWork.Repository<User>().AnyAsync(x => x.Email == request.Email, cancellationToken);
                if (emailExists)
                {
                    logger.LogInformation("Email already exists: {Email}", request.Email);
                    return Result<Guid>.Fail("Email already exists");
                }

                if (!Enum.TryParse<UserRole>(request.Role, out var userRole) || !Enum.IsDefined(userRole))
                {
                    logger.LogInformation("Invalid role: {Role}", request.Role);
                    return Result<Guid>.Fail("Invalid role");
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


                var user = new User(resEmail.Value, resPhone.Value, request.FirstName, request.LastName, userRole);
                user.SetPassword(passwordHash, salt);
                _unitOfWork.Repository<User>().Add(user);

                if (userRole == UserRole.Lawyer)
                {
                    if (string.IsNullOrWhiteSpace(request.BarNumber))
                    {
                        return Result<Guid>.Fail("Bar number is required for lawyer registration");
                    }

                    var attachResult = user.AttachLawyerProfile(request.BarNumber);
                    if (!attachResult.IsSuccess)
                    {
                        return Result<Guid>.Fail(attachResult.Error);
                    }
                }

                await _unitOfWork.SaveChangesAsync(cancellationToken);
                logger.LogInformation("User registered successfully: {UserId}", user.Id);
                return Result<Guid>.Ok(user.Id);
            }
            
        }
    }
}
