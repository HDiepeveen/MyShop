using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class AttributeValuePersistenceWriterTests
{
    [Fact]
    public void Write_RejectsNullArguments()
    {
        var source = TextAttributeValue.Create(AttributeDefinitionId.New(), "text");

        Assert.Throws<ArgumentNullException>(() => AttributeValuePersistenceWriter.Write(
            null!, new ProductAttributeValuePersistence()));
        Assert.Throws<ArgumentNullException>(() => AttributeValuePersistenceWriter.Write(
            source, (ProductAttributeValuePersistence)null!));
        Assert.Throws<ArgumentNullException>(() => AttributeValuePersistenceWriter.Write(
            null!, new ProductVariantAttributeValuePersistence()));
        Assert.Throws<ArgumentNullException>(() => AttributeValuePersistenceWriter.Write(
            source, (ProductVariantAttributeValuePersistence)null!));
    }

    [Fact]
    public void Write_Product_MapsEveryDataTypeAndClearsDirtyPayload()
    {
        foreach (var testCase in Cases())
        {
            var target = DirtyProductTarget();
            var productId = target.ProductId;
            var definitionId = target.AttributeDefinitionId;
            var ordinal = target.Ordinal;

            AttributeValuePersistenceWriter.Write(testCase.Source, target);

            Assert.Equal(productId, target.ProductId);
            Assert.Equal(definitionId, target.AttributeDefinitionId);
            Assert.Equal(ordinal, target.Ordinal);
            AssertPayload(target, testCase);
        }
    }

    [Fact]
    public void Write_Variant_MapsEveryDataTypeAndClearsDirtyPayload()
    {
        foreach (var testCase in Cases())
        {
            var target = DirtyVariantTarget();
            var variantId = target.ProductVariantId;
            var definitionId = target.AttributeDefinitionId;
            var ordinal = target.Ordinal;

            AttributeValuePersistenceWriter.Write(testCase.Source, target);

            Assert.Equal(variantId, target.ProductVariantId);
            Assert.Equal(definitionId, target.AttributeDefinitionId);
            Assert.Equal(ordinal, target.Ordinal);
            AssertPayload(target, testCase);
        }
    }

    [Fact]
    public void Write_Decimal_UsesExactCanonicalCoefficientAndScale()
    {
        var value = DecimalAttributeValue.Create(AttributeDefinitionId.New(), -987.65432100m);
        var product = ProductTarget();
        var variant = VariantTarget();

        AttributeValuePersistenceWriter.Write(value, product);
        AttributeValuePersistenceWriter.Write(value, variant);

        Assert.Equal(-987654321m, product.DecimalCoefficient);
        Assert.Equal((byte)6, product.DecimalScale);
        Assert.Equal(-987654321m, variant.DecimalCoefficient);
        Assert.Equal((byte)6, variant.DecimalScale);
        Assert.Equal(value.Value, DecimalAttributeValuePersistenceConverter.ToDomain(
            product.DecimalCoefficient!.Value, product.DecimalScale!.Value));
        Assert.Equal(value.Value, DecimalAttributeValuePersistenceConverter.ToDomain(
            variant.DecimalCoefficient!.Value, variant.DecimalScale!.Value));
    }

    [Fact]
    public void Write_Scalar_RemovesValidMultiChoiceChildren()
    {
        var source = BooleanAttributeValue.Create(AttributeDefinitionId.New(), false);
        var product = DirtyProductTarget();
        var variant = DirtyVariantTarget();

        AttributeValuePersistenceWriter.Write(source, product);
        AttributeValuePersistenceWriter.Write(source, variant);

        Assert.Empty(product.MultiChoiceValues);
        Assert.Empty(variant.MultiChoiceValues);
        Assert.False(product.BooleanValue);
        Assert.False(variant.BooleanValue);
    }

    [Fact]
    public void Write_ProductMultiChoice_ReconcilesByOrdinalWithoutMutatingKeys()
    {
        var target = ProductTarget();
        target.TextValue = "stale";
        var reused = ProductChild(target, 0, "old first");
        var obsolete = ProductChild(target, 4, "obsolete");
        target.MultiChoiceValues.Add(reused);
        target.MultiChoiceValues.Add(obsolete);
        var source = MultiChoice("first", "second");

        AttributeValuePersistenceWriter.Write(source, target);

        Assert.Equal(AttributeDataType.MultiChoice, target.DataType);
        AssertAllScalarPayloadsNull(target);
        var children = target.MultiChoiceValues.OrderBy(child => child.Ordinal).ToArray();
        Assert.Equal([0, 1], children.Select(child => child.Ordinal));
        Assert.Equal(["first", "second"], children.Select(child => child.Value));
        Assert.Same(reused, children[0]);
        Assert.Equal(0, reused.Ordinal);
        Assert.DoesNotContain(obsolete, target.MultiChoiceValues);
        Assert.Same(target, children[1].AttributeValue);
        Assert.Equal(target.ProductId, children[1].ProductId);
        Assert.Equal(target.AttributeDefinitionId, children[1].AttributeDefinitionId);
    }

    [Fact]
    public void Write_VariantMultiChoice_ReconcilesByOrdinalWithoutMutatingKeys()
    {
        var target = VariantTarget();
        target.TextValue = "stale";
        var reused = VariantChild(target, 1, "old second");
        var obsolete = VariantChild(target, -1, "obsolete");
        target.MultiChoiceValues.Add(reused);
        target.MultiChoiceValues.Add(obsolete);
        var source = MultiChoice("first", "second");

        AttributeValuePersistenceWriter.Write(source, target);

        Assert.Equal(AttributeDataType.MultiChoice, target.DataType);
        AssertAllScalarPayloadsNull(target);
        var children = target.MultiChoiceValues.OrderBy(child => child.Ordinal).ToArray();
        Assert.Equal([0, 1], children.Select(child => child.Ordinal));
        Assert.Equal(["first", "second"], children.Select(child => child.Value));
        Assert.Same(reused, children[1]);
        Assert.Equal(1, reused.Ordinal);
        Assert.DoesNotContain(obsolete, target.MultiChoiceValues);
        Assert.Same(target, children[0].AttributeValue);
        Assert.Equal(target.ProductVariantId, children[0].ProductVariantId);
        Assert.Equal(target.AttributeDefinitionId, children[0].AttributeDefinitionId);
    }

    [Fact]
    public void Write_Product_RejectsMalformedChildrenBeforeAnyMutation()
    {
        AssertProductFailureIsAtomic(target =>
            target.MultiChoiceValues.Add(WithOwner(ProductChild(target, 1, "value"), Guid.NewGuid())));
        AssertProductFailureIsAtomic(target =>
            target.MultiChoiceValues.Add(WithDefinition(ProductChild(target, 1, "value"), Guid.NewGuid())));
        AssertProductFailureIsAtomic(target =>
        {
            target.MultiChoiceValues.Add(ProductChild(target, 0, "first"));
            target.MultiChoiceValues.Add(ProductChild(target, 0, "second"));
        });
        AssertProductFailureIsAtomic(target => target.MultiChoiceValues.Add(null!));
        AssertProductFailureIsAtomic(target => target.MultiChoiceValues = null!);
    }

    [Fact]
    public void Write_Variant_RejectsMalformedChildrenBeforeAnyMutation()
    {
        AssertVariantFailureIsAtomic(target =>
            target.MultiChoiceValues.Add(WithOwner(VariantChild(target, 1, "value"), Guid.NewGuid())));
        AssertVariantFailureIsAtomic(target =>
            target.MultiChoiceValues.Add(WithDefinition(VariantChild(target, 1, "value"), Guid.NewGuid())));
        AssertVariantFailureIsAtomic(target =>
        {
            target.MultiChoiceValues.Add(VariantChild(target, 0, "first"));
            target.MultiChoiceValues.Add(VariantChild(target, 0, "second"));
        });
        AssertVariantFailureIsAtomic(target => target.MultiChoiceValues.Add(null!));
        AssertVariantFailureIsAtomic(target => target.MultiChoiceValues = null!);
    }

    private static void AssertProductFailureIsAtomic(Action<ProductAttributeValuePersistence> corrupt)
    {
        var target = DirtyProductTarget();
        corrupt(target);
        var dataType = target.DataType;
        var text = target.TextValue;
        var integer = target.IntegerValue;
        var coefficient = target.DecimalCoefficient;
        var scale = target.DecimalScale;
        var boolean = target.BooleanValue;
        var date = target.DateValue;
        var choice = target.ChoiceValue;
        var children = target.MultiChoiceValues?.ToArray();

        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceWriter.Write(
            TextAttributeValue.Create(AttributeDefinitionId.New(), "replacement"), target));

        Assert.Equal(dataType, target.DataType);
        Assert.Equal(text, target.TextValue);
        Assert.Equal(integer, target.IntegerValue);
        Assert.Equal(coefficient, target.DecimalCoefficient);
        Assert.Equal(scale, target.DecimalScale);
        Assert.Equal(boolean, target.BooleanValue);
        Assert.Equal(date, target.DateValue);
        Assert.Equal(choice, target.ChoiceValue);
        if (children is null)
            Assert.Null(target.MultiChoiceValues);
        else
            Assert.Equal(children, target.MultiChoiceValues);
    }

    private static void AssertVariantFailureIsAtomic(Action<ProductVariantAttributeValuePersistence> corrupt)
    {
        var target = DirtyVariantTarget();
        corrupt(target);
        var dataType = target.DataType;
        var text = target.TextValue;
        var integer = target.IntegerValue;
        var coefficient = target.DecimalCoefficient;
        var scale = target.DecimalScale;
        var boolean = target.BooleanValue;
        var date = target.DateValue;
        var choice = target.ChoiceValue;
        var children = target.MultiChoiceValues?.ToArray();

        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceWriter.Write(
            TextAttributeValue.Create(AttributeDefinitionId.New(), "replacement"), target));

        Assert.Equal(dataType, target.DataType);
        Assert.Equal(text, target.TextValue);
        Assert.Equal(integer, target.IntegerValue);
        Assert.Equal(coefficient, target.DecimalCoefficient);
        Assert.Equal(scale, target.DecimalScale);
        Assert.Equal(boolean, target.BooleanValue);
        Assert.Equal(date, target.DateValue);
        Assert.Equal(choice, target.ChoiceValue);
        if (children is null)
            Assert.Null(target.MultiChoiceValues);
        else
            Assert.Equal(children, target.MultiChoiceValues);
    }

    private static IReadOnlyList<ValueCase> Cases()
    {
        var textId = AttributeDefinitionId.New();
        var integerId = AttributeDefinitionId.New();
        var decimalId = AttributeDefinitionId.New();
        var booleanId = AttributeDefinitionId.New();
        var dateId = AttributeDefinitionId.New();
        var choiceId = AttributeDefinitionId.New();

        return
        [
            new(TextAttributeValue.Create(textId, "  text  "), AttributeDataType.Text, Text: "  text  "),
            new(IntegerAttributeValue.Create(integerId, long.MinValue), AttributeDataType.Integer, Integer: long.MinValue),
            new(DecimalAttributeValue.Create(decimalId, 1.2300m), AttributeDataType.Decimal,
                DecimalCoefficient: 123m, DecimalScale: 2),
            new(BooleanAttributeValue.Create(booleanId, false), AttributeDataType.Boolean, Boolean: false),
            new(DateAttributeValue.Create(dateId, new DateOnly(2026, 9, 16)), AttributeDataType.Date,
                Date: new DateOnly(2026, 9, 16)),
            new(ChoiceAttributeValue.Create(choiceId, ChoiceValue.Create(" Choice ")), AttributeDataType.Choice,
                Choice: " Choice "),
            new(MultiChoice("first", "second"), AttributeDataType.MultiChoice,
                MultiChoiceValues: ["first", "second"])
        ];
    }

    private static MultiChoiceAttributeValue MultiChoice(params string[] values) =>
        MultiChoiceAttributeValue.Create(
            AttributeDefinitionId.New(),
            values.Select(ChoiceValue.Create));

    private static ProductAttributeValuePersistence ProductTarget() => new()
    {
        ProductId = Guid.NewGuid(),
        AttributeDefinitionId = Guid.NewGuid(),
        Ordinal = 42
    };

    private static ProductVariantAttributeValuePersistence VariantTarget() => new()
    {
        ProductVariantId = Guid.NewGuid(),
        AttributeDefinitionId = Guid.NewGuid(),
        Ordinal = 42
    };

    private static ProductAttributeValuePersistence DirtyProductTarget()
    {
        var target = ProductTarget();
        target.DataType = (AttributeDataType)999;
        target.TextValue = "stale text";
        target.IntegerValue = 123;
        target.DecimalCoefficient = 456m;
        target.DecimalScale = 7;
        target.BooleanValue = true;
        target.DateValue = new DateOnly(2000, 1, 1);
        target.ChoiceValue = "stale choice";
        target.MultiChoiceValues.Add(ProductChild(target, 0, "stale child"));
        return target;
    }

    private static ProductVariantAttributeValuePersistence DirtyVariantTarget()
    {
        var target = VariantTarget();
        target.DataType = (AttributeDataType)999;
        target.TextValue = "stale text";
        target.IntegerValue = 123;
        target.DecimalCoefficient = 456m;
        target.DecimalScale = 7;
        target.BooleanValue = true;
        target.DateValue = new DateOnly(2000, 1, 1);
        target.ChoiceValue = "stale choice";
        target.MultiChoiceValues.Add(VariantChild(target, 0, "stale child"));
        return target;
    }

    private static ProductAttributeMultiChoiceValuePersistence ProductChild(
        ProductAttributeValuePersistence target, int ordinal, string value) => new()
    {
        ProductId = target.ProductId,
        AttributeDefinitionId = target.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value,
        AttributeValue = target
    };

    private static ProductVariantAttributeMultiChoiceValuePersistence VariantChild(
        ProductVariantAttributeValuePersistence target, int ordinal, string value) => new()
    {
        ProductVariantId = target.ProductVariantId,
        AttributeDefinitionId = target.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value,
        AttributeValue = target
    };

    private static ProductAttributeMultiChoiceValuePersistence WithOwner(
        ProductAttributeMultiChoiceValuePersistence child, Guid ownerId)
    {
        child.ProductId = ownerId;
        return child;
    }

    private static ProductAttributeMultiChoiceValuePersistence WithDefinition(
        ProductAttributeMultiChoiceValuePersistence child, Guid definitionId)
    {
        child.AttributeDefinitionId = definitionId;
        return child;
    }

    private static ProductVariantAttributeMultiChoiceValuePersistence WithOwner(
        ProductVariantAttributeMultiChoiceValuePersistence child, Guid ownerId)
    {
        child.ProductVariantId = ownerId;
        return child;
    }

    private static ProductVariantAttributeMultiChoiceValuePersistence WithDefinition(
        ProductVariantAttributeMultiChoiceValuePersistence child, Guid definitionId)
    {
        child.AttributeDefinitionId = definitionId;
        return child;
    }

    private static void AssertPayload(ProductAttributeValuePersistence target, ValueCase expected)
    {
        Assert.Equal(expected.DataType, target.DataType);
        Assert.Equal(expected.Text, target.TextValue);
        Assert.Equal(expected.Integer, target.IntegerValue);
        Assert.Equal(expected.DecimalCoefficient, target.DecimalCoefficient);
        Assert.Equal(expected.DecimalScale, target.DecimalScale);
        Assert.Equal(expected.Boolean, target.BooleanValue);
        Assert.Equal(expected.Date, target.DateValue);
        Assert.Equal(expected.Choice, target.ChoiceValue);
        Assert.Equal(expected.MultiChoiceValues ?? [], target.MultiChoiceValues
            .OrderBy(child => child.Ordinal).Select(child => child.Value));
    }

    private static void AssertPayload(ProductVariantAttributeValuePersistence target, ValueCase expected)
    {
        Assert.Equal(expected.DataType, target.DataType);
        Assert.Equal(expected.Text, target.TextValue);
        Assert.Equal(expected.Integer, target.IntegerValue);
        Assert.Equal(expected.DecimalCoefficient, target.DecimalCoefficient);
        Assert.Equal(expected.DecimalScale, target.DecimalScale);
        Assert.Equal(expected.Boolean, target.BooleanValue);
        Assert.Equal(expected.Date, target.DateValue);
        Assert.Equal(expected.Choice, target.ChoiceValue);
        Assert.Equal(expected.MultiChoiceValues ?? [], target.MultiChoiceValues
            .OrderBy(child => child.Ordinal).Select(child => child.Value));
    }

    private static void AssertAllScalarPayloadsNull(ProductAttributeValuePersistence target)
    {
        Assert.Null(target.TextValue);
        Assert.Null(target.IntegerValue);
        Assert.Null(target.DecimalCoefficient);
        Assert.Null(target.DecimalScale);
        Assert.Null(target.BooleanValue);
        Assert.Null(target.DateValue);
        Assert.Null(target.ChoiceValue);
    }

    private static void AssertAllScalarPayloadsNull(ProductVariantAttributeValuePersistence target)
    {
        Assert.Null(target.TextValue);
        Assert.Null(target.IntegerValue);
        Assert.Null(target.DecimalCoefficient);
        Assert.Null(target.DecimalScale);
        Assert.Null(target.BooleanValue);
        Assert.Null(target.DateValue);
        Assert.Null(target.ChoiceValue);
    }

    private sealed record ValueCase(
        AttributeValue Source,
        AttributeDataType DataType,
        string? Text = null,
        long? Integer = null,
        decimal? DecimalCoefficient = null,
        byte? DecimalScale = null,
        bool? Boolean = null,
        DateOnly? Date = null,
        string? Choice = null,
        IReadOnlyList<string>? MultiChoiceValues = null);
}
