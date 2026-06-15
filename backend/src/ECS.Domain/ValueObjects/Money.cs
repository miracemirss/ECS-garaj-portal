using ECS.Domain.Exceptions;

namespace ECS.Domain.ValueObjects;

/// <summary>
/// Monetary amount with currency. Available domain primitive for cost handling;
/// entities persist plain decimals + the company currency, but services may use
/// this for safe arithmetic. Amounts are non-negative and rounded to 2 decimals.
/// </summary>
public sealed class Money : ValueObject
{
    public decimal Amount { get; }
    public string Currency { get; }

    private Money(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    public static Money Create(decimal amount, string currency = "TRY")
    {
        if (amount < 0)
        {
            throw new DomainException("Money amount cannot be negative.");
        }
        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new DomainException("Currency is required.");
        }

        return new Money(decimal.Round(amount, 2), currency.ToUpperInvariant());
    }

    public static Money Zero(string currency = "TRY") => new(0m, currency.ToUpperInvariant());

    public Money Add(Money other)
    {
        EnsureSameCurrency(other);
        return new Money(Amount + other.Amount, Currency);
    }

    public Money Multiply(decimal factor) => new(decimal.Round(Amount * factor, 2), Currency);

    private void EnsureSameCurrency(Money other)
    {
        if (Currency != other.Currency)
        {
            throw new DomainException($"Cannot operate on different currencies ({Currency} vs {other.Currency}).");
        }
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
