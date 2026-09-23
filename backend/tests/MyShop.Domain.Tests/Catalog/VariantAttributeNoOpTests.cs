using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class VariantAttributeNoOpTests
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
        var variant = product.Variants.Single();
        var id = AttributeDefinitionId.New();
        var original = Value(kind, id, false);
        var other = TextAttributeValue.Create(AttributeDefinitionId.New(), "Other");
        Assert.True(product.SetVariantAttributeValue(variant.Id, original));
        Assert.True(product.SetVariantAttributeValue(variant.Id, other));

        Assert.False(product.SetVariantAttributeValue(variant.Id, Value(kind, id, false)));
        Assert.Same(original, variant.AttributeValues.First());
        Assert.Same(other, variant.AttributeValues.Last());
        var changed = Value(kind, id, true);
        Assert.True(product.SetVariantAttributeValue(variant.Id, changed));
        Assert.Same(changed, variant.AttributeValues.First());
        Assert.Same(other, variant.AttributeValues.Last());
        Assert.Equal(2, variant.AttributeValues.Count);
    }

    [Fact]
    public void Set_TypeChangeIsNotANoOpAndNullDoesNotMutate()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variant = product.Variants.Single();
        var id = AttributeDefinitionId.New();
        product.SetVariantAttributeValue(variant.Id, IntegerAttributeValue.Create(id, 1));
        var changed = DecimalAttributeValue.Create(id, 1m);
        Assert.True(product.SetVariantAttributeValue(variant.Id, changed));
        Assert.Throws<ArgumentNullException>(() => product.SetVariantAttributeValue(variant.Id, null!));
        Assert.Same(changed, Assert.Single(variant.AttributeValues));
    }

    [Fact]
    public void Set_EqualValuesOnOtherVariantDoNotSuppressAssignment()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        var id = AttributeDefinitionId.New();
        var firstValue = TextAttributeValue.Create(id, "Red");
        product.SetVariantAttributeValue(first.Id, firstValue);
        Assert.True(product.SetVariantAttributeValue(second.Id, TextAttributeValue.Create(id, "Red")));
        Assert.Same(firstValue, Assert.Single(first.AttributeValues));
        Assert.Single(second.AttributeValues);
        Assert.Empty(product.AttributeValues);
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
