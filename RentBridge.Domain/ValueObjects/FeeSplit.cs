namespace RentBridge.Domain.ValueObjects
{
    /// <summary>
    /// Immutable breakdown of how a gross rent payment is split between
    /// the platform commission, the lawyer's legal fee share, and the
    /// landlord payout. Always sums to the gross amount.
    /// </summary>
    public sealed record FeeSplit
    {
        public Money PlatformCommission { get; }
        public Money LegalFeeShare { get; }
        public Money LandlordPayout { get; }

        private FeeSplit(Money platformCommission, Money legalFeeShare, Money landlordPayout)
        {
            PlatformCommission = platformCommission;
            LegalFeeShare = legalFeeShare;
            LandlordPayout = landlordPayout;
        }

        /// <summary>Creates a split that must sum to the gross amount.</summary>
        public static FeeSplit Create(Money gross, Money platformCommission, Money legalFeeShare)
        {
            var landlordPayout = gross.Subtract(platformCommission).Subtract(legalFeeShare);
            if (landlordPayout.Amount < 0)
                throw new ArgumentException("Commissions plus legal fee exceed the gross amount.");

            return new FeeSplit(platformCommission, legalFeeShare, landlordPayout);
        }
    }
}
