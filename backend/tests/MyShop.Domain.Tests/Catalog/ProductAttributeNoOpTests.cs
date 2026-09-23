using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductAttributeNoOpTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void Set_RecognizesEqualValuesAndPreservesIdentityAndOrder(int kind)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var id = AttributeDefinitionId.New();
        var original = Value(kind, id, false);
        var other = TextAttributeValue.Create(AttributeDefinitionId.New(), "Other");
        Assert.True(product.SetAttributeValue(original));
        Assert.True(product.SetAttributeValue(other));

        Assert.False(product.SetAttributeValue(Value(kind, id, false)));
        Assert.Same(original, product.AttributeValues.First());
        Assert.Same(other, product.AttributeValues.Last());
        var changed = Value(kind, id, true);
        Assert.True(product.SetAttributeValue(changed));
        Assert.Same(changed, product.AttributeValues.First());
        Assert.Same(other, product.AttributeValues.Last());
        Assert.Equal(2, product.AttributeValues.Count);
    }

    [Fact]
    public void Set_TypeChangeIsNotANoOpAndNullDoesNotMutate()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var id = AttributeDefinitionId.New();
        product.SetAttributeValue(IntegerAttributeValue.Create(id, 1));
        var changed = DecimalAttributeValue.Create(id, 1m);
        Assert.True(product.SetAttributeValue(changed));
        Assert.Throws<ArgumentNullException>(() => product.SetAttributeValue(null!));
        Assert.Same(changed, Assert.Single(product.AttributeValues));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Set_NumericallyEqualDecimalsKeepExistingRepresentation(bool onVariant)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        var id = AttributeDefinitionId.New();
        var original = DecimalAttributeValue.Create(id, 1.0m);
        if (onVariant) product.SetVariantAttributeValue(variant.Id, original);
        else product.SetAttributeValue(original);
        var equivalent = DecimalAttributeValue.Create(id, 1.00m);
        Assert.False(onVariant ? product.SetVariantAttributeValue(variant.Id, equivalent) :
            product.SetAttributeValue(equivalent));
        var retained = Assert.IsType<DecimalAttributeValue>(onVariant ? variant.AttributeValues.Single() :
            product.AttributeValues.Single());
        Assert.Same(original, retained);
        Assert.Equal(decimal.GetBits(1.0m), decimal.GetBits(retained.Value));
    }

    private static AttributeValue Value(int kind, AttributeDefinitionId id, bool changed) => kind switch
    {
        0 => TextAttributeValue.Create(id, changed ? " Red " : "Red"),
        1 => IntegerAttributeValue.Create(id, changed ? 2 : 1),
        2 => DecimalAttributeValue.Create(id, changed ? 1.01m : 1m),
        3 => BooleanAttributeValue.Create(id, changed),
        4 => DateAttributeValue.Create(id, new DateOnly(2026, 1, changed ? 2 : 1)),
        5 => ChoiceAttributeValue.Create(id, ChoiceValue.Create(changed ? "Blue" : "Red")),
        _ => MultiChoiceAttributeValue.Create(id, changed
            ? [ChoiceValue.Create("Blue"), ChoiceValue.Create("Red")]
            : [ChoiceValue.Create("Red"), ChoiceValue.Create("Blue")])
    };
}
