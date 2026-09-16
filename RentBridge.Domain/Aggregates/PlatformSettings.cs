using RentBridge.Domain.Common;

namespace RentBridge.Domain.Aggregates;

/// <summary>
/// Singleton platform-wide settings (a single row). As of MVP it holds the
/// escrow fee split percentages, editable only by an admin.
/// </summary>
public class PlatformSettings : Entity<Guid>
{
    public const string SingletonId = "00000000-0000-0000-0000-000000000001";

    public decimal PlatformCommissionRate { get; private set; }
    public decimal LegalFeeRate { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private PlatformSettings() { }   // EF

    private PlatformSettings(decimal commissionRate, decimal legalFeeRate)
    {
        Id = Guid.Parse(SingletonId);
        PlatformCommissionRate = commissionRate;
        LegalFeeRate = legalFeeRate;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public static Result<PlatformSettings> Create(decimal commissionRate, decimal legalFeeRate)
    {
        var validation = ValidateRates(commissionRate, legalFeeRate);
        if (!validation.IsSuccess) return Result<PlatformSettings>.Fail(validation.Error!);

        return Result<PlatformSettings>.Ok(new PlatformSettings(commissionRate, legalFeeRate));
    }

    public Result Update(decimal commissionRate, decimal legalFeeRate)
    {
        var validation = ValidateRates(commissionRate, legalFeeRate);
        if (!validation.IsSuccess) return validation;

        PlatformCommissionRate = commissionRate;
        LegalFeeRate = legalFeeRate;
        UpdatedAt = DateTimeOffset.UtcNow;
        return Result.Ok();
    }

    private static Result ValidateRates(decimal commissionRate, decimal legalFeeRate)
    {
        if (commissionRate < 0 || legalFeeRate < 0)
            return Result.Fail("Fee rates cannot be negative.");
        if (commissionRate >= 100 || legalFeeRate >= 100)
            return Result.Fail("Fee rates must be below 100%.");
        if (commissionRate + legalFeeRate >= 100)
            return Result.Fail("Commission plus legal fee must be below 100%.");

        return Result.Ok();
    }
}