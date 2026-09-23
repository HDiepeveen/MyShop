using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class AttributeNoOpRoundTripTests
{
    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 1)]
    [InlineData(false, 2)]
    [InlineData(false, 3)]
    [InlineData(false, 4)]
    [InlineData(false, 5)]
    [InlineData(false, 6)]
    [InlineData(true, 0)]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(true, 3)]
    [InlineData(true, 4)]
    [InlineData(true, 5)]
    [InlineData(true, 6)]
    public void RehydratedValue_IsEqualToFreshInputWithoutReplacingValue(bool onVariant, int kind)
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Variant");
        var variantId = product.Variants.Single().Id;
        var definitionId = AttributeDefinitionId.New();
        var input = Value(kind, definitionId, false);
        if (onVariant) product.SetVariantAttributeValue(variantId, input);
        else product.SetAttributeValue(input);
        var revision = Guid.NewGuid();
        var persistence = new ProductPersistence { Id = product.Id.Value, Version = revision };
        ProductPersistenceSynchronizer.Synchronize(product, persistence);
        var snapshot = ProductPersistenceMapper.ToSnapshot(persistence);
        var original = onVariant ? snapshot.Product.Variants.Single().AttributeValues.Single() :
            snapshot.Product.AttributeValues.Single();
        Assert.NotSame(input, original);
        var freshInput = Value(kind, definitionId, false);

        var changed = onVariant ? snapshot.Product.SetVariantAttributeValue(variantId, freshInput) :
            snapshot.Product.SetAttributeValue(freshInput);

        Assert.False(changed);
        Assert.Same(original, onVariant ? snapshot.Product.Variants.Single().AttributeValues.Single() :
            snapshot.Product.AttributeValues.Single());
        Assert.Equal(revision, snapshot.ConcurrencyToken.Revision);
        Assert.Equal(revision, persistence.Version);
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
