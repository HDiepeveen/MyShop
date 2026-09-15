using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog.Abstractions;

public sealed class ProductConcurrencyTokenTests
{
    [Fact]
    public void Create_WithValidValues_PreservesProductIdAndRevision()
    {
        var id = ProductId.New();
        var revision = Guid.NewGuid();
        var token = ProductConcurrencyToken.Create(id, revision);
        Assert.Equal(id, token.ProductId);
        Assert.Equal(revision, token.Revision);
    }

    [Fact]
    public void Create_WithDefaultProductId_Throws() =>
        Assert.Throws<ArgumentException>(() => ProductConcurrencyToken.Create(default, Guid.NewGuid()));

    [Fact]
    public void Create_WithEmptyRevision_Throws() =>
        Assert.Throws<ArgumentException>(() => ProductConcurrencyToken.Create(ProductId.New(), Guid.Empty));

    [Fact]
    public void Equality_WithSameValues_IsEqualAndHasSameHashCode()
    {
        var id = ProductId.New();
        var revision = Guid.NewGuid();
        var first = ProductConcurrencyToken.Create(id, revision);
        var second = ProductConcurrencyToken.Create(id, revision);
        Assert.Equal(first, second);
        Assert.True(first == second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void Equality_WithDifferentProductIds_IsNotEqual()
    {
        var revision = Guid.NewGuid();
        Assert.NotEqual(ProductConcurrencyToken.Create(ProductId.New(), revision),
            ProductConcurrencyToken.Create(ProductId.New(), revision));
    }

    [Fact]
    public void Equality_WithDifferentRevisions_IsNotEqual()
    {
        var id = ProductId.New();
        Assert.NotEqual(ProductConcurrencyToken.Create(id, Guid.NewGuid()),
            ProductConcurrencyToken.Create(id, Guid.NewGuid()));
    }
}
