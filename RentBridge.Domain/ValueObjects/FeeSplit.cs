using RentBridge.Domain.Common;

namespace RentBridge.Domain.ValueObjects
{
    /// <summary>
    /// Immutable breakdown of how a gross rent payment is split between
    /// the platform commission, the lawyer's legal fee share, and the
    /// landlord payout. Always sums to the gross amount.
    /// </summary>
    public sealed record FeeSplit
    {
        public Money PlatformCommission { get; private set; }
        public Money LegalFeeShare { get; private set; }
        public Money LandlordPayout { get; private set; }

        private FeeSplit() { } // EF

        private FeeSplit(Money platformCommission, Money legalFeeShare, Money landlordPayout)
        {
            PlatformCommission = platformCommission;
            LegalFeeShare = legalFeeShare;
            LandlordPayout = landlordPayout;
        }

        /// <summary>Creates a split that must sum to the gross amount.</summary>
        public static Result<FeeSplit> Create(Money gross, Money platformCommission, Money legalFeeShare)
        {
            var commission = gross.Subtract(platformCommission);
            if (!commission.IsSuccess) return Result<FeeSplit>.Fail(commission.Error!);

            var landlordPayout = commission.Value.Subtract(legalFeeShare);
            if (!landlordPayout.IsSuccess) return Result<FeeSplit>.Fail(landlordPayout.Error!);

            if (landlordPayout.Value.Amount < 0)
                return Result<FeeSplit>.Fail("Commissions plus legal fee exceed the gross amount.");

            return Result<FeeSplit>.Ok(new FeeSplit(platformCommission, legalFeeShare, landlordPayout.Value));
        }
    }
}