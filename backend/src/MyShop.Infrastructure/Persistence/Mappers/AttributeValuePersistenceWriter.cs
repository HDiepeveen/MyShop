using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class AttributeValuePersistenceWriter
{
    internal static void Write(
        AttributeValue source,
        ProductAttributeValuePersistence target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var children = ValidateChildren(target);
        var payload = CreatePayload(source);

        ApplyPayload(target, payload);

        if (payload.MultiChoiceValues is null)
        {
            RemoveAll(children, target.MultiChoiceValues);
            return;
        }

        SynchronizeChildren(target, children, payload.MultiChoiceValues);
    }

    internal static void Write(
        AttributeValue source,
        ProductVariantAttributeValuePersistence target)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(target);

        var children = ValidateChildren(target);
        var payload = CreatePayload(source);

        ApplyPayload(target, payload);

        if (payload.MultiChoiceValues is null)
        {
            RemoveAll(children, target.MultiChoiceValues);
            return;
        }

        SynchronizeChildren(target, children, payload.MultiChoiceValues);
    }

    private static Payload CreatePayload(AttributeValue source) => source switch
    {
        TextAttributeValue value => new(AttributeDataType.Text, TextValue: value.Value),
        IntegerAttributeValue value => new(AttributeDataType.Integer, IntegerValue: value.Value),
        DecimalAttributeValue value => CreateDecimalPayload(value),
        BooleanAttributeValue value => new(AttributeDataType.Boolean, BooleanValue: value.Value),
        DateAttributeValue value => new(AttributeDataType.Date, DateValue: value.Value),
        ChoiceAttributeValue value => new(AttributeDataType.Choice, ChoiceValue: value.Value.Value),
        MultiChoiceAttributeValue value => new(
            AttributeDataType.MultiChoice,
            MultiChoiceValues: value.Values.Select(choice => choice.Value).ToArray()),
        _ => throw new InvalidOperationException(
            $"Unsupported attribute value type '{source.GetType().FullName}'.")
    };

    private static Payload CreateDecimalPayload(DecimalAttributeValue value)
    {
        var persisted = DecimalAttributeValuePersistenceConverter.ToPersistence(value.Value);
        return new Payload(
            AttributeDataType.Decimal,
            DecimalCoefficient: persisted.Coefficient,
            DecimalScale: persisted.Scale);
    }

    private static void ApplyPayload(ProductAttributeValuePersistence target, Payload payload)
    {
        target.DataType = payload.DataType;
        target.TextValue = payload.TextValue;
        target.IntegerValue = payload.IntegerValue;
        target.DecimalCoefficient = payload.DecimalCoefficient;
        target.DecimalScale = payload.DecimalScale;
        target.BooleanValue = payload.BooleanValue;
        target.DateValue = payload.DateValue;
        target.ChoiceValue = payload.ChoiceValue;
    }

    private static void ApplyPayload(ProductVariantAttributeValuePersistence target, Payload payload)
    {
        target.DataType = payload.DataType;
        target.TextValue = payload.TextValue;
        target.IntegerValue = payload.IntegerValue;
        target.DecimalCoefficient = payload.DecimalCoefficient;
        target.DecimalScale = payload.DecimalScale;
        target.BooleanValue = payload.BooleanValue;
        target.DateValue = payload.DateValue;
        target.ChoiceValue = payload.ChoiceValue;
    }

    private static IReadOnlyList<ProductAttributeMultiChoiceValuePersistence> ValidateChildren(
        ProductAttributeValuePersistence target)
    {
        var children = target.MultiChoiceValues
            ?? throw InvalidStructure(target.AttributeDefinitionId, "Multi-choice collection must not be null.");

        if (children.Any(child => child is null))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice collection must not contain null children.");

        var snapshot = children.ToList();
        if (snapshot.Any(child => child.ProductId != target.ProductId))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child Product ID does not match its parent.");
        if (snapshot.Any(child => child.AttributeDefinitionId != target.AttributeDefinitionId))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child Attribute definition ID does not match its parent.");
        if (snapshot.GroupBy(child => child.Ordinal).Any(group => group.Count() > 1))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child ordinals must be distinct.");

        return snapshot;
    }

    private static IReadOnlyList<ProductVariantAttributeMultiChoiceValuePersistence> ValidateChildren(
        ProductVariantAttributeValuePersistence target)
    {
        var children = target.MultiChoiceValues
            ?? throw InvalidStructure(target.AttributeDefinitionId, "Multi-choice collection must not be null.");

        if (children.Any(child => child is null))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice collection must not contain null children.");

        var snapshot = children.ToList();
        if (snapshot.Any(child => child.ProductVariantId != target.ProductVariantId))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child Product variant ID does not match its parent.");
        if (snapshot.Any(child => child.AttributeDefinitionId != target.AttributeDefinitionId))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child Attribute definition ID does not match its parent.");
        if (snapshot.GroupBy(child => child.Ordinal).Any(group => group.Count() > 1))
            throw InvalidStructure(target.AttributeDefinitionId,
                "Multi-choice child ordinals must be distinct.");

        return snapshot;
    }

    private static void SynchronizeChildren(
        ProductAttributeValuePersistence target,
        IReadOnlyList<ProductAttributeMultiChoiceValuePersistence> existing,
        IReadOnlyList<string> desiredValues)
    {
        var byOrdinal = existing.ToDictionary(child => child.Ordinal);

        foreach (var child in existing.Where(child => child.Ordinal < 0 || child.Ordinal >= desiredValues.Count))
            target.MultiChoiceValues.Remove(child);

        for (var ordinal = 0; ordinal < desiredValues.Count; ordinal++)
        {
            if (byOrdinal.TryGetValue(ordinal, out var child))
            {
                child.Value = desiredValues[ordinal];
                continue;
            }

            target.MultiChoiceValues.Add(new ProductAttributeMultiChoiceValuePersistence
            {
                ProductId = target.ProductId,
                AttributeDefinitionId = target.AttributeDefinitionId,
                Ordinal = ordinal,
                Value = desiredValues[ordinal],
                AttributeValue = target
            });
        }
    }

    private static void SynchronizeChildren(
        ProductVariantAttributeValuePersistence target,
        IReadOnlyList<ProductVariantAttributeMultiChoiceValuePersistence> existing,
        IReadOnlyList<string> desiredValues)
    {
        var byOrdinal = existing.ToDictionary(child => child.Ordinal);

        foreach (var child in existing.Where(child => child.Ordinal < 0 || child.Ordinal >= desiredValues.Count))
            target.MultiChoiceValues.Remove(child);

        for (var ordinal = 0; ordinal < desiredValues.Count; ordinal++)
        {
            if (byOrdinal.TryGetValue(ordinal, out var child))
            {
                child.Value = desiredValues[ordinal];
                continue;
            }

            target.MultiChoiceValues.Add(new ProductVariantAttributeMultiChoiceValuePersistence
            {
                ProductVariantId = target.ProductVariantId,
                AttributeDefinitionId = target.AttributeDefinitionId,
                Ordinal = ordinal,
                Value = desiredValues[ordinal],
                AttributeValue = target
            });
        }
    }

    private static void RemoveAll<T>(IReadOnlyList<T> existing, ICollection<T> target)
    {
        foreach (var child in existing)
            target.Remove(child);
    }

    private static InvalidOperationException InvalidStructure(Guid attributeDefinitionId, string message) =>
        new($"Invalid persisted attribute value '{attributeDefinitionId}': {message}");

    private sealed record Payload(
        AttributeDataType DataType,
        string? TextValue = null,
        long? IntegerValue = null,
        decimal? DecimalCoefficient = null,
        byte? DecimalScale = null,
        bool? BooleanValue = null,
        DateOnly? DateValue = null,
        string? ChoiceValue = null,
        IReadOnlyList<string>? MultiChoiceValues = null);
}
