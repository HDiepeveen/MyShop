using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Persistence.Repositories;

public sealed class CategoryRepositoryTests
{
    [Fact]
    public void Constructor_RejectsNullContext() =>
        Assert.Throws<ArgumentNullException>(() => new CategoryRepository(null!));

    [Fact]
    public void ListQuery_UsesSqlServerSummaryProjectionWithDeterministicOrdering()
    {
        using var context = CreateContext();

        var query = CategoryRepository.ListQuery(context.Set<CategoryPersistence>(), null);
        var expression = query.Expression.ToString();
        var sql = query.ToQueryString();

        Assert.Contains("AsNoTracking", expression);
        Assert.Contains("OrderBy", expression);
        Assert.Contains("ThenBy", expression);
        Assert.Contains("FROM [Categories]", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.DoesNotContain("JOIN", sql);
    }

    [Fact]
    public void ListQuery_WithSearchTerm_UsesSqlServerNamePredicate()
    {
        using var context = CreateContext();

        var sql = CategoryRepository.ListQuery(
            context.Set<CategoryPersistence>(), "shirt").ToQueryString();

        Assert.Contains("[c].[Name]", sql);
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
