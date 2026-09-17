using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
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

        var query = CategoryRepository.ListQuery(context.Set<CategoryPersistence>(), null, null, false);
        var expression = query.Expression.ToString();
        var sql = query.ToQueryString();

        Assert.Contains("AsNoTracking", expression);
        Assert.Contains("OrderBy", expression);
        Assert.Contains("ThenBy", expression);
        Assert.Contains("FROM [Categories]", sql);
        Assert.Contains("ORDER BY", sql);
        Assert.DoesNotContain("JOIN", sql);
        Assert.Contains("COUNT(*)", sql);
    }

    [Fact]
    public void ListQuery_WithSearchTerm_UsesSqlServerNamePredicate()
    {
        using var context = CreateContext();

        var sql = CategoryRepository.ListQuery(
            context.Set<CategoryPersistence>(), "shirt", null, false).ToQueryString();

        Assert.Contains("[c].[Name]", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void ListQuery_WithParentId_UsesSqlServerParentPredicate()
    {
        using var context = CreateContext();

        var sql = CategoryRepository.ListQuery(
            context.Set<CategoryPersistence>(), null, CategoryId.New(), false).ToQueryString();

        Assert.Contains("[c].[ParentCategoryId]", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void ListQuery_WithRootsOnly_UsesSqlServerNullParentPredicate()
    {
        using var context = CreateContext();

        var sql = CategoryRepository.ListQuery(
            context.Set<CategoryPersistence>(), null, null, true).ToQueryString();

        Assert.Contains("[c].[ParentCategoryId] IS NULL", sql);
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;
        return new MyShopDbContext(options);
    }
}
