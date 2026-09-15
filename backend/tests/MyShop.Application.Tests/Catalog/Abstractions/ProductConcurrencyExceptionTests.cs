using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Application.Tests.Catalog.Abstractions;

public sealed class ProductConcurrencyExceptionTests
{
    [Fact]
    public void Constructor_WithValidProductId_PreservesIdAndExplainsConflict()
    {
        var id = ProductId.New();
        var exception = new ProductConcurrencyException(id);
        Assert.Equal(id, exception.ProductId);
        Assert.Contains(id.ToString(), exception.Message);
        Assert.Contains("changed or was deleted", exception.Message);
        Assert.Null(exception.InnerException);
    }

    [Fact]
    public void Constructor_WithDefaultProductId_Throws() =>
        Assert.Throws<ArgumentException>(() => new ProductConcurrencyException(default));
}
