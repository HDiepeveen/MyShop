using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Persistence.Repositories;

public sealed class ProductSkuLookupTests
{
    [Fact]
    public void Constructor_RejectsNullContext() =>
        Assert.Throws<ArgumentNullException>(() => new ProductSkuLookup(null!));

    [Fact]
    public void OwnerQuery_UsesNoTrackingSqlServerProjectionAndSkuPredicate()
    {
        using var context = CreateContext();

        var query = ProductSkuLookup.OwnerQuery(context.ProductVariants, Sku.Create("SKU-001"));
        var expression = query.Expression.ToString();
        var sql = query.ToQueryString();

        Assert.Contains("AsNoTracking", expression);
        Assert.Contains("FROM [ProductVariants]", sql);
        Assert.Contains("[p].[Sku]", sql);
        Assert.Contains("WHERE", sql);
        Assert.Contains("[p].[ProductId]", sql);
        Assert.Contains("[p].[Id]", sql);
    }

    [Fact]
    public void OwnerQuery_RejectsNullArguments()
    {
        using var context = CreateContext();
        Assert.Throws<ArgumentNullException>(() =>
            ProductSkuLookup.OwnerQuery(null!, Sku.Create("SKU-001")));
        Assert.Throws<ArgumentNullException>(() =>
            ProductSkuLookup.OwnerQuery(context.ProductVariants, null!));
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;
        return new MyShopDbContext(options);
    }
}
