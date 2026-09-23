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

    [Fact]
    public void ListQuery_AppliesSqlPagingAfterFiltersWithStableOrdering()
    {
        using var context = CreateContext();
        var sql = CategoryRepository.ListQuery(context.Categories, "shirt", CategoryId.New(), false, 10, 5).ToQueryString();
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("[c].[ParentCategoryId]", sql);
        Assert.Contains("ORDER BY [c].[Name], [c].[Id]", sql);
        Assert.Contains("OFFSET", sql);
        Assert.Contains("FETCH NEXT", sql);
        Assert.Contains("= 10;", sql);
        Assert.Contains("= 5;", sql);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Theory]
    [InlineData(0, 2, 2)]
    [InlineData(2, 2, 1)]
    [InlineData(3, 2, 0)]
    [InlineData(2147483647, 2, 0)]
    public void ListQuery_PagesFilteredRowsAndPreservesSummaryCounts(int offset, int limit, int expectedCount)
    {
        var parent = CategoryId.New();
        var rows = Enumerable.Range(1, 3).Select(index => new CategoryPersistence
        {
            Id = new Guid(index, 0, 0, new byte[8]), Name = "Match", ParentCategoryId = parent.Value,
            Children = [new CategoryPersistence { Id = Guid.NewGuid(), Name = "Child" }]
        }).Reverse().ToList();
        rows.Add(new CategoryPersistence { Id = Guid.NewGuid(), Name = "Excluded" });

        var result = CategoryRepository.ListQuery(rows.AsQueryable(), "Match", parent, false, offset, limit).ToArray();

        Assert.Equal(expectedCount, result.Length);
        Assert.All(result, item => Assert.Equal(1, item.DirectChildCount));
        Assert.Equal(Enumerable.Range(1, 3).Skip(offset).Take(limit).Select(index => new Guid(index, 0, 0, new byte[8])),
            result.Select(item => item.Id));
    }

    [Fact]
    public void ListQuery_DefaultPageIsBounded()
    {
        var rows = Enumerable.Range(1, 60).Select(index => new CategoryPersistence
            { Id = new Guid(index, 0, 0, new byte[8]), Name = "Category" }).AsQueryable();
        Assert.Equal(50, CategoryRepository.ListQuery(rows, null, null, false).Count());
    }

    [Fact]
    public void ParentIdQuery_UsesSqlServerScalarProjectionWithoutConnection()
    {
        using var context = CreateContext();
        var sql = CategoryRepository.ParentIdQuery(
            context.Set<CategoryPersistence>(), Guid.NewGuid()).ToQueryString();
        Assert.Contains("SELECT [c].[ParentCategoryId]", sql);
        Assert.Contains("WHERE [c].[Id]", sql);
    }

    [Fact]
    public void UsageQueries_UseSqlServerPredicatesWithoutConnection()
    {
        using var context = CreateContext();
        var id = CategoryId.New();
        var childrenSql = CategoryRepository.DirectChildCountQuery(context.Categories, id).ToQueryString();
        var assignmentsSql = CategoryRepository.ProductAssignmentCountQuery(context.ProductCategories, id)
            .ToQueryString();
        Assert.Contains("[c].[ParentCategoryId]", childrenSql);
        Assert.Contains("[p].[CategoryId]", assignmentsSql);
        Assert.Contains("WHERE", childrenSql);
        Assert.Contains("WHERE", assignmentsSql);
    }

    private static MyShopDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .Options;
        return new MyShopDbContext(options);
    }
}
