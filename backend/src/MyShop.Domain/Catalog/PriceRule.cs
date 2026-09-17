namespace MyShop.Domain.Catalog;

public sealed class PriceRule
{
    private PriceRule(Guid id, string name, PriceAdjustmentType adjustmentType, decimal value, int priority, DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        Id = id; Name = name; AdjustmentType = adjustmentType; Value = value; Priority = priority; StartsAt = startsAt; EndsAt = endsAt;
    }

    public Guid Id { get; }
    public string Name { get; }
    public PriceAdjustmentType AdjustmentType { get; }
    public decimal Value { get; }
    public int Priority { get; }
    public DateTimeOffset? StartsAt { get; }
    public DateTimeOffset? EndsAt { get; }

    public static PriceRule Create(string name, PriceAdjustmentType adjustmentType, decimal value, int priority,
        DateTimeOffset? startsAt = null, DateTimeOffset? endsAt = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!Enum.IsDefined(adjustmentType)) throw new ArgumentOutOfRangeException(nameof(adjustmentType));
        if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
        if (adjustmentType == PriceAdjustmentType.PercentageDiscount && value > 100) throw new ArgumentOutOfRangeException(nameof(value));
        if (startsAt is not null && endsAt is not null && endsAt < startsAt) throw new ArgumentException("End must not precede start.", nameof(endsAt));
        return new PriceRule(Guid.NewGuid(), name.Trim(), adjustmentType, decimal.Round(value, 2), priority, startsAt, endsAt);
    }

    public bool IsActiveAt(DateTimeOffset instant) =>
        (StartsAt is null || instant >= StartsAt) && (EndsAt is null || instant <= EndsAt);

    public Money Apply(Money basePrice)
    {
        if (basePrice == default) throw new ArgumentException("Base price must be specified.", nameof(basePrice));
        var discount = AdjustmentType == PriceAdjustmentType.PercentageDiscount
            ? basePrice.Amount * Value / 100m
            : Value;
        return Money.Create(Math.Max(0, basePrice.Amount - discount), basePrice.Currency);
    }
}
