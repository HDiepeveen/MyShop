namespace MyShop.Domain.Catalog;

public readonly record struct Money
{
    private Money(decimal amount, string currency) { Amount = amount; Currency = currency; }
    public decimal Amount { get; }
    public string Currency { get; }
    public static Money Create(decimal amount, string currency)
    {
        if (amount < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        var normalized = currency.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(c => c is < 'A' or > 'Z'))
            throw new ArgumentException("Currency must be a three-letter ISO code.", nameof(currency));
        return new Money(decimal.Round(amount, 2, MidpointRounding.ToEven), normalized);
    }
}
