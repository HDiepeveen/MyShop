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

    [Fact]
    public void ListQuery_AppliesSqlPagingAfterSearchWithStableOrdering()
    {
        using var context = CreateContext();
        var sql = ProductTypeRepository.ListQuery(context.ProductTypes, "shirt", 10, 5).ToQueryString();
        Assert.Contains("WHERE", sql);
        Assert.Contains("LIKE", sql);
        Assert.Contains("ORDER BY [p].[Name], [p].[Id]", sql);
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
    public void ListQuery_PagesFilteredRowsAndPreservesDefinitionCounts(int offset, int limit, int expectedCount)
    {
        var rows = Enumerable.Range(1, 3).Select(index => new ProductTypePersistence
        {
            Id = new Guid(index, 0, 0, new byte[8]), Name = "Match",
            AttributeDefinitions = [new AttributeDefinitionPersistence { Id = Guid.NewGuid() }]
        }).Reverse().ToList();
        rows.Add(new ProductTypePersistence { Id = Guid.NewGuid(), Name = "Excluded" });

        var result = ProductTypeRepository.ListQuery(rows.AsQueryable(), "Match", offset, limit).ToArray();

        Assert.Equal(expectedCount, result.Length);
        Assert.All(result, item => Assert.Equal(1, item.AttributeDefinitionCount));
        Assert.Equal(Enumerable.Range(1, 3).Skip(offset).Take(limit).Select(index => new Guid(index, 0, 0, new byte[8])),
            result.Select(item => item.Id));
    }

    [Fact]
    public void ListQuery_DefaultPageIsBounded()
    {
        var rows = Enumerable.Range(1, 60).Select(index => new ProductTypePersistence
            { Id = new Guid(index, 0, 0, new byte[8]), Name = "Type" }).AsQueryable();
        Assert.Equal(50, ProductTypeRepository.ListQuery(rows, null).Count());
    }

    [Fact]
    public void CompleteGraph_UsesSqlServerAttributeDefinitionInclude()
    {
        using var context = CreateContext();

        var query = ProductTypeRepository.CompleteGraph(context.Set<ProductTypePersistence>());
        var sql = query.ToQueryString();

        Assert.Contains("LEFT JOIN [AttributeDefinitions]", sql);
        Assert.Contains("ORDER BY", sql);
    }

    [Fact]
    public void ProductsQuery_UsesSqlServerProductTypePredicate()
    {
        using var context = CreateContext();
        var sql = ProductTypeRepository.ProductsQuery(
            context.Products, MyShop.Domain.Catalog.ProductTypeId.New()).ToQueryString();
        Assert.Contains("FROM [Products]", sql);
        Assert.Contains("[p].[ProductTypeId]", sql);
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
