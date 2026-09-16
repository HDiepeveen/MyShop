using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class AttributeValuePersistenceMapperTests
{
    [Fact]
    public void ToDomain_Product_MapsEveryDataTypeExactly()
    {
        var rows = CreateProductRows();

        var values = rows.Select(AttributeValuePersistenceMapper.ToDomain).ToArray();

        Assert.Equal("  Text  ", Assert.IsType<TextAttributeValue>(values[0]).Value);
        Assert.Equal(long.MinValue, Assert.IsType<IntegerAttributeValue>(values[1]).Value);
        Assert.Equal(-12.345m, Assert.IsType<DecimalAttributeValue>(values[2]).Value);
        Assert.False(Assert.IsType<BooleanAttributeValue>(values[3]).Value);
        Assert.True(Assert.IsType<BooleanAttributeValue>(values[4]).Value);
        Assert.Equal(new DateOnly(2026, 9, 16), Assert.IsType<DateAttributeValue>(values[5]).Value);
        Assert.Equal(" Choice ", Assert.IsType<ChoiceAttributeValue>(values[6]).Value.Value);
        Assert.Equal(["first", "second"],
            Assert.IsType<MultiChoiceAttributeValue>(values[7]).Values.Select(value => value.Value));
    }

    [Fact]
    public void ToDomain_Variant_MapsEveryDataTypeExactly()
    {
        var rows = CreateVariantRows();

        var values = rows.Select(AttributeValuePersistenceMapper.ToDomain).ToArray();

        Assert.Equal("  Text  ", Assert.IsType<TextAttributeValue>(values[0]).Value);
        Assert.Equal(long.MinValue, Assert.IsType<IntegerAttributeValue>(values[1]).Value);
        Assert.Equal(-12.345m, Assert.IsType<DecimalAttributeValue>(values[2]).Value);
        Assert.False(Assert.IsType<BooleanAttributeValue>(values[3]).Value);
        Assert.True(Assert.IsType<BooleanAttributeValue>(values[4]).Value);
        Assert.Equal(new DateOnly(2026, 9, 16), Assert.IsType<DateAttributeValue>(values[5]).Value);
        Assert.Equal(" Choice ", Assert.IsType<ChoiceAttributeValue>(values[6]).Value.Value);
        Assert.Equal(["first", "second"],
            Assert.IsType<MultiChoiceAttributeValue>(values[7]).Values.Select(value => value.Value));
    }

    [Fact]
    public void ToDomain_RejectsNullRows()
    {
        Assert.Throws<ArgumentNullException>(() =>
            AttributeValuePersistenceMapper.ToDomain((ProductAttributeValuePersistence)null!));
        Assert.Throws<ArgumentNullException>(() =>
            AttributeValuePersistenceMapper.ToDomain((ProductVariantAttributeValuePersistence)null!));
    }

    [Fact]
    public void ToDomain_RejectsInvalidOwnerDefinitionAndDiscriminator()
    {
        var product = ProductText();
        product.ProductId = Guid.Empty;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductText();
        product.AttributeDefinitionId = Guid.Empty;
        Assert.Throws<ArgumentException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductText();
        product.DataType = (AttributeDataType)999;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));

        var variant = VariantText();
        variant.ProductVariantId = Guid.Empty;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantText();
        variant.AttributeDefinitionId = Guid.Empty;
        Assert.Throws<ArgumentException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantText();
        variant.DataType = (AttributeDataType)999;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
    }

    [Fact]
    public void ToDomain_RejectsMissingPayloadForEveryDataType()
    {
        foreach (var dataType in Enum.GetValues<AttributeDataType>())
        {
            var product = ProductBase(dataType);
            var variant = VariantBase(dataType);

            Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
            Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        }
    }

    [Fact]
    public void ToDomain_RejectsUnrelatedPayloadAndScalarChildren()
    {
        var product = ProductText();
        product.IntegerValue = 1;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductText();
        product.MultiChoiceValues.Add(ProductChild(product, 0, "choice"));
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));

        var variant = VariantText();
        variant.IntegerValue = 1;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantText();
        variant.MultiChoiceValues.Add(VariantChild(variant, 0, "choice"));
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
    }

    [Fact]
    public void ToDomain_RejectsInvalidMultiChoiceCollectionShape()
    {
        var product = ProductBase(AttributeDataType.MultiChoice);
        product.TextValue = "unexpected";
        product.MultiChoiceValues.Add(ProductChild(product, 0, "choice"));
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductBase(AttributeDataType.MultiChoice);
        product.MultiChoiceValues = null!;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductBase(AttributeDataType.MultiChoice);
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product.MultiChoiceValues.Add(null!);
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));

        var variant = VariantBase(AttributeDataType.MultiChoice);
        variant.TextValue = "unexpected";
        variant.MultiChoiceValues.Add(VariantChild(variant, 0, "choice"));
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantBase(AttributeDataType.MultiChoice);
        variant.MultiChoiceValues = null!;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantBase(AttributeDataType.MultiChoice);
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant.MultiChoiceValues.Add(null!);
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
    }

    [Fact]
    public void ToDomain_RejectsDuplicateOrdinalAndMismatchedChildIdentity()
    {
        var product = ProductMultiChoice();
        product.MultiChoiceValues.ElementAt(1).Ordinal = product.MultiChoiceValues.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductMultiChoice();
        product.MultiChoiceValues.ElementAt(0).ProductId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));
        product = ProductMultiChoice();
        product.MultiChoiceValues.ElementAt(0).AttributeDefinitionId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));

        var variant = VariantMultiChoice();
        variant.MultiChoiceValues.ElementAt(1).Ordinal = variant.MultiChoiceValues.ElementAt(0).Ordinal;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantMultiChoice();
        variant.MultiChoiceValues.ElementAt(0).ProductVariantId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
        variant = VariantMultiChoice();
        variant.MultiChoiceValues.ElementAt(0).AttributeDefinitionId = Guid.NewGuid();
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ToDomain_InvalidMultiChoiceValuePropagatesDomainValidation(string? value)
    {
        var product = ProductBase(AttributeDataType.MultiChoice);
        product.MultiChoiceValues.Add(ProductChild(product, 0, value!));
        Assert.IsAssignableFrom<ArgumentException>(Record.Exception(() =>
            AttributeValuePersistenceMapper.ToDomain(product)));

        var variant = VariantBase(AttributeDataType.MultiChoice);
        variant.MultiChoiceValues.Add(VariantChild(variant, 0, value!));
        Assert.IsAssignableFrom<ArgumentException>(Record.Exception(() =>
            AttributeValuePersistenceMapper.ToDomain(variant)));
    }

    [Fact]
    public void ToDomain_MultiChoiceUsesDomainDuplicateAndOrdinalValueSemantics()
    {
        var duplicateProduct = ProductBase(AttributeDataType.MultiChoice);
        duplicateProduct.MultiChoiceValues.Add(ProductChild(duplicateProduct, -2, "same"));
        duplicateProduct.MultiChoiceValues.Add(ProductChild(duplicateProduct, 8, "same"));
        Assert.Throws<ArgumentException>(() => AttributeValuePersistenceMapper.ToDomain(duplicateProduct));

        var duplicateVariant = VariantBase(AttributeDataType.MultiChoice);
        duplicateVariant.MultiChoiceValues.Add(VariantChild(duplicateVariant, -2, "same"));
        duplicateVariant.MultiChoiceValues.Add(VariantChild(duplicateVariant, 8, "same"));
        Assert.Throws<ArgumentException>(() => AttributeValuePersistenceMapper.ToDomain(duplicateVariant));

        var product = ProductBase(AttributeDataType.MultiChoice);
        product.MultiChoiceValues.Add(ProductChild(product, 9, "same"));
        product.MultiChoiceValues.Add(ProductChild(product, -3, " Same "));
        var productValue = Assert.IsType<MultiChoiceAttributeValue>(AttributeValuePersistenceMapper.ToDomain(product));
        Assert.Equal([" Same ", "same"], productValue.Values.Select(value => value.Value));

        var variant = VariantBase(AttributeDataType.MultiChoice);
        variant.MultiChoiceValues.Add(VariantChild(variant, 9, "same"));
        variant.MultiChoiceValues.Add(VariantChild(variant, -3, " Same "));
        var variantValue = Assert.IsType<MultiChoiceAttributeValue>(AttributeValuePersistenceMapper.ToDomain(variant));
        Assert.Equal([" Same ", "same"], variantValue.Values.Select(value => value.Value));
    }

    [Fact]
    public void ToDomain_MalformedDecimalPropagatesConverterFailure()
    {
        var product = ProductBase(AttributeDataType.Decimal);
        product.DecimalCoefficient = 10m;
        product.DecimalScale = 1;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(product));

        var variant = VariantBase(AttributeDataType.Decimal);
        variant.DecimalCoefficient = 123.0m;
        variant.DecimalScale = 2;
        Assert.Throws<InvalidOperationException>(() => AttributeValuePersistenceMapper.ToDomain(variant));
    }

    private static ProductAttributeValuePersistence[] CreateProductRows()
    {
        var text = ProductText();
        var integer = ProductBase(AttributeDataType.Integer); integer.IntegerValue = long.MinValue;
        var decimalValue = ProductBase(AttributeDataType.Decimal); decimalValue.DecimalCoefficient = -12345m; decimalValue.DecimalScale = 3;
        var falseValue = ProductBase(AttributeDataType.Boolean); falseValue.BooleanValue = false;
        var trueValue = ProductBase(AttributeDataType.Boolean); trueValue.BooleanValue = true;
        var date = ProductBase(AttributeDataType.Date); date.DateValue = new DateOnly(2026, 9, 16);
        var choice = ProductBase(AttributeDataType.Choice); choice.ChoiceValue = " Choice ";
        return [text, integer, decimalValue, falseValue, trueValue, date, choice, ProductMultiChoice()];
    }

    private static ProductVariantAttributeValuePersistence[] CreateVariantRows()
    {
        var text = VariantText();
        var integer = VariantBase(AttributeDataType.Integer); integer.IntegerValue = long.MinValue;
        var decimalValue = VariantBase(AttributeDataType.Decimal); decimalValue.DecimalCoefficient = -12345m; decimalValue.DecimalScale = 3;
        var falseValue = VariantBase(AttributeDataType.Boolean); falseValue.BooleanValue = false;
        var trueValue = VariantBase(AttributeDataType.Boolean); trueValue.BooleanValue = true;
        var date = VariantBase(AttributeDataType.Date); date.DateValue = new DateOnly(2026, 9, 16);
        var choice = VariantBase(AttributeDataType.Choice); choice.ChoiceValue = " Choice ";
        return [text, integer, decimalValue, falseValue, trueValue, date, choice, VariantMultiChoice()];
    }

    private static ProductAttributeValuePersistence ProductText()
    {
        var row = ProductBase(AttributeDataType.Text);
        row.TextValue = "  Text  ";
        return row;
    }

    private static ProductVariantAttributeValuePersistence VariantText()
    {
        var row = VariantBase(AttributeDataType.Text);
        row.TextValue = "  Text  ";
        return row;
    }

    private static ProductAttributeValuePersistence ProductMultiChoice()
    {
        var row = ProductBase(AttributeDataType.MultiChoice);
        row.MultiChoiceValues.Add(ProductChild(row, 10, "second"));
        row.MultiChoiceValues.Add(ProductChild(row, -4, "first"));
        return row;
    }

    private static ProductVariantAttributeValuePersistence VariantMultiChoice()
    {
        var row = VariantBase(AttributeDataType.MultiChoice);
        row.MultiChoiceValues.Add(VariantChild(row, 10, "second"));
        row.MultiChoiceValues.Add(VariantChild(row, -4, "first"));
        return row;
    }

    private static ProductAttributeValuePersistence ProductBase(AttributeDataType dataType) => new()
    {
        ProductId = Guid.NewGuid(),
        AttributeDefinitionId = Guid.NewGuid(),
        DataType = dataType
    };

    private static ProductVariantAttributeValuePersistence VariantBase(AttributeDataType dataType) => new()
    {
        ProductVariantId = Guid.NewGuid(),
        AttributeDefinitionId = Guid.NewGuid(),
        DataType = dataType
    };

    private static ProductAttributeMultiChoiceValuePersistence ProductChild(
        ProductAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductId = parent.ProductId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value
    };

    private static ProductVariantAttributeMultiChoiceValuePersistence VariantChild(
        ProductVariantAttributeValuePersistence parent, int ordinal, string value) => new()
    {
        ProductVariantId = parent.ProductVariantId,
        AttributeDefinitionId = parent.AttributeDefinitionId,
        Ordinal = ordinal,
        Value = value
    };
}
