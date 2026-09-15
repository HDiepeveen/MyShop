using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog.Abstractions;

public sealed class ProductSnapshotTests
{
    [Fact]
    public void Constructor_WithMatchingToken_PreservesReferences()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");
        var token = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
        var snapshot = new ProductSnapshot(product, token);
        Assert.Same(product, snapshot.Product);
        Assert.Same(token, snapshot.ConcurrencyToken);
    }

    [Fact]
    public void Constructor_WithNullProduct_Throws()
    {
        var token = ProductConcurrencyToken.Create(ProductId.New(), Guid.NewGuid());
        Assert.Throws<ArgumentNullException>(() => new ProductSnapshot(null!, token));
    }

    [Fact]
    public void Constructor_WithNullToken_Throws()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");
        Assert.Throws<ArgumentNullException>(() => new ProductSnapshot(product, null!));
    }

    [Fact]
    public void Constructor_WithMismatchedToken_Throws()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Standard");
        var token = ProductConcurrencyToken.Create(ProductId.New(), Guid.NewGuid());
        Assert.Throws<ArgumentException>(() => new ProductSnapshot(product, token));
    }
}
