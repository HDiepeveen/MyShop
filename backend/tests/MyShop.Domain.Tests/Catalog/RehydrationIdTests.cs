using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class RehydrationIdTests
{
    [Fact]
    public void From_PreservesSuppliedValues()
    {
        var value = Guid.NewGuid();

        Assert.Equal(value, ProductId.From(value).Value);
        Assert.Equal(value, ProductVariantId.From(value).Value);
        Assert.Equal(value, ProductTypeId.From(value).Value);
        Assert.Equal(value, AttributeDefinitionId.From(value).Value);
        Assert.Equal(value, CategoryId.From(value).Value);
    }

    [Fact]
    public void From_WithEmptyGuid_ThrowsForEveryIdType()
    {
        Assert.Throws<ArgumentException>(() => ProductId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => ProductVariantId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => ProductTypeId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => AttributeDefinitionId.From(Guid.Empty));
        Assert.Throws<ArgumentException>(() => CategoryId.From(Guid.Empty));
    }

    [Fact]
    public void New_StillProducesNonEmptyValues()
    {
        Assert.NotEqual(Guid.Empty, ProductId.New().Value);
        Assert.NotEqual(Guid.Empty, ProductVariantId.New().Value);
        Assert.NotEqual(Guid.Empty, ProductTypeId.New().Value);
        Assert.NotEqual(Guid.Empty, AttributeDefinitionId.New().Value);
        Assert.NotEqual(Guid.Empty, CategoryId.New().Value);
    }

    [Fact]
    public void From_PreservesValueEquality()
    {
        var value = Guid.NewGuid();

        Assert.Equal(ProductId.From(value), ProductId.From(value));
        Assert.Equal(ProductVariantId.From(value), ProductVariantId.From(value));
        Assert.Equal(ProductTypeId.From(value), ProductTypeId.From(value));
        Assert.Equal(AttributeDefinitionId.From(value), AttributeDefinitionId.From(value));
        Assert.Equal(CategoryId.From(value), CategoryId.From(value));
    }
}
