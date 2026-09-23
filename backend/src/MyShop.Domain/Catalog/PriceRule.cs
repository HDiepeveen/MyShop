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
        var normalizedName = name.Trim();
        if (normalizedName.Length > 200)
            throw new ArgumentException("Price rule name must not exceed 200 characters.", nameof(name));
        if (!Enum.IsDefined(adjustmentType)) throw new ArgumentOutOfRangeException(nameof(adjustmentType));
        var roundedValue = decimal.Round(value, 2, MidpointRounding.ToEven);
        if (roundedValue <= 0 || roundedValue > 9999999999999999.99m) throw new ArgumentOutOfRangeException(nameof(value));
        if (adjustmentType == PriceAdjustmentType.PercentageDiscount && value > 100) throw new ArgumentOutOfRangeException(nameof(value));
        if (startsAt is not null && endsAt is not null && endsAt < startsAt) throw new ArgumentException("End must not precede start.", nameof(endsAt));
        return new PriceRule(Guid.NewGuid(), normalizedName, adjustmentType, roundedValue, priority, startsAt, endsAt);
    }

    internal static PriceRule Rehydrate(Guid id, string name, PriceAdjustmentType adjustmentType, decimal value, int priority,
        DateTimeOffset? startsAt, DateTimeOffset? endsAt)
    {
        if (id == Guid.Empty) throw new ArgumentException("Price rule ID must not be empty.", nameof(id));
        var rule = Create(name, adjustmentType, value, priority, startsAt, endsAt);
        return new PriceRule(id, rule.Name, rule.AdjustmentType, rule.Value, rule.Priority, rule.StartsAt, rule.EndsAt);
    }

    internal PriceRule WithChanges(string name, PriceAdjustmentType adjustmentType, decimal value,
        int priority, DateTimeOffset? startsAt, DateTimeOffset? endsAt) =>
        Rehydrate(Id, name, adjustmentType, value, priority, startsAt, endsAt);

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
