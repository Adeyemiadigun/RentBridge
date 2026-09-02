using RentBridge.Domain.Common;
using RentBridge.Domain.Enums;
using RentBridge.Domain.ValueObjects;

namespace RentBridge.Domain.Aggregates.Users;

public class User : Entity<Guid>
{
    public Email Email { get; private set; }
    public PhoneNumber Phone { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; } = string.Empty;
    public UserRole Role { get; private set; }
    public bool IdentityVerified { get; private set; }
    public Guid? KycVerificationId { get; private set; }
    public string PasswordHash { get; private set; }
    public string PasswordSalt { get; private set; }
    public LawyerProfile? LawyerProfile { get; private set; }

    private User() { }   // EF

    public User(Email email, PhoneNumber phone, string firstName, string lastName, UserRole role)
        : this()
    {
        Id = Guid.NewGuid();
        Email = email;
        Phone = phone;
        FirstName = firstName;
        LastName = lastName;
        Role = role;
    }

    public void MarkIdentityVerified(Guid kycId)
    {
        KycVerificationId = kycId;
        IdentityVerified = true;
    }

    public void SetPassword(string passwordHash, string passwordSalt)
    {
        PasswordHash = passwordHash;
        PasswordSalt = passwordSalt;
    }

    public Result AttachLawyerProfile(string barNumber)
    {
        if (Role != UserRole.Lawyer)
            return Result.Fail("Only lawyer accounts can have a lawyer profile.");
        if (LawyerProfile is not null)
            return Result.Fail("A lawyer profile already exists for this user.");
        LawyerProfile = new LawyerProfile(barNumber);
        return Result.Ok();
    }

}

public record IdentityVerified(Guid UserId, Guid KycVerificationId) : IDomainEvent;
