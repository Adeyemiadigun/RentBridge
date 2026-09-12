using RentBridge.Domain.Common;

namespace RentBridge.Domain.ValueObjects
{
    public sealed record Money
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; }

        private Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        public static Result<Money> Naira(decimal amount)
        {
            if (amount < 0) return Result<Money>.Fail("Amount cannot be negative");
            return Result<Money>.Ok(new(amount, "NGN"));
        }

        public Result<Money> Add(Money other) => SameCurrency(other, () => Naira(Amount + other.Amount));
        public Result<Money> Subtract(Money other) => SameCurrency(other, () => Naira(Amount - other.Amount));

        private Result<Money> SameCurrency(Money other, Func<Result<Money>> f) =>
            Currency == other.Currency ? f() : Result<Money>.Fail("Currency mismatch");
    }
}