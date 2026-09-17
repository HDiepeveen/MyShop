using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Persistence.Repositories;

public sealed class ProductListRepositoryTests
{
    [Fact]
    public void Constructor_RejectsNullContext() =>
        Assert.Throws<ArgumentNullException>(() => new ProductListRepository(null!));

    [Fact]
    public void ItemsQuery_UsesSqlServerPagedSummaryProjectionWithDeterministicOrdering()
    {
        using var context = CreateContext();

        var query = ProductListRepository.ItemsQuery(context.Set<ProductPersistence>(), 20, 10);
        var expression = query.Expression.ToString();
        var sql = query.ToQueryString();

        Assert.Contains("AsNoTracking", expression);
        Assert.Contains("OrderBy", expression);
        Assert.Contains("ThenBy", expression);
        Assert.Contains("Skip", expression);
        Assert.Contains("Take", expression);
        Assert.Contains("FROM [Products]", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("COUNT(*)", sql);
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;
        return new MyShopDbContext(options);
    }
}
