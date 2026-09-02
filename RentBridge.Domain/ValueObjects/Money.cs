namespace RentBridge.Domain.ValueObjects
{
    public sealed record Money
    {
        public decimal Amount { get; init; }
        public string Currency { get; init; } 

        private Money(decimal amount, string currency)
        {
            if (amount < 0) throw new ArgumentException("Amount cannot be negative");
            Amount = amount;
            Currency = currency;
        }
        public static Money Naira(decimal amount) => new(amount, "NGN");

        public Money Add(Money other) => SameCurrency(other, () => Naira(Amount + other.Amount));
        public Money Subtract(Money other) => SameCurrency(other, () => Naira(Amount - other.Amount));

        private Money SameCurrency(Money other, Func<Money> f) =>
            Currency == other.Currency ? f() : throw new InvalidOperationException("Currency mismatch");
    }
}

