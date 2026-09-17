using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Persistence.Repositories;

public sealed class ProductTypeRepositoryTests
{
    [Fact]
    public void Constructor_RejectsNullContext() =>
        Assert.Throws<ArgumentNullException>(() => new ProductTypeRepository(null!));

    [Fact]
    public void ListQuery_UsesSqlServerSummaryProjectionWithDeterministicOrdering()
    {
        using var context = CreateContext();

        var query = ProductTypeRepository.ListQuery(context.Set<ProductTypePersistence>(), null);
        var expression = query.Expression.ToString();
        var sql = query.ToQueryString();

        Assert.Contains("AsNoTracking", expression);
        Assert.Contains("OrderBy", expression);
        Assert.Contains("ThenBy", expression);
        Assert.Contains("FROM [ProductTypes]", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.DoesNotContain("JOIN", sql);
        Assert.Contains("COUNT(*)", sql);
    }

    [Fact]
    public void ListQuery_WithSearchTerm_UsesSqlServerNamePredicate()
    {
        using var context = CreateContext();

        var sql = ProductTypeRepository.ListQuery(
            context.Set<ProductTypePersistence>(), "cloth").ToQueryString();

        Assert.Contains("[p].[Name]", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("WHERE", sql);
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;
        return new MyShopDbContext(options);
    }
}
