using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class CategoryPersistenceMapperTests
{
    [Fact]
    public void ToDomain_WithNullPersistence_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => CategoryPersistenceMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomain_RootCategoryPreservesState()
    {
        var id = Guid.NewGuid();

        var category = CategoryPersistenceMapper.ToDomain(new CategoryPersistence
        {
            Id = id,
            Name = "Electronics",
            ParentCategoryId = null
        });

        Assert.Equal(id, category.Id.Value);
        Assert.Equal("Electronics", category.Name);
        Assert.Null(category.ParentCategoryId);
        Assert.True(category.IsRoot);
    }

    [Fact]
    public void ToDomain_ChildCategoryPreservesParentId()
    {
        var id = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var category = CategoryPersistenceMapper.ToDomain(new CategoryPersistence
        {
            Id = id,
            Name = "Computers",
            ParentCategoryId = parentId
        });

        Assert.Equal(id, category.Id.Value);
        Assert.Equal("Computers", category.Name);
        Assert.Equal(parentId, category.ParentCategoryId!.Value.Value);
    }

    [Fact]
    public void ToDomain_IgnoresParentAndChildrenNavigations()
    {
        var persistence = new CategoryPersistence
        {
            Id = Guid.NewGuid(),
            Name = "Laptops",
            ParentCategoryId = Guid.NewGuid(),
            Parent = new CategoryPersistence { Id = Guid.NewGuid(), Name = "Ignored" },
            Children = [new CategoryPersistence { Id = Guid.NewGuid(), Name = "Ignored child" }]
        };

        var category = CategoryPersistenceMapper.ToDomain(persistence);

        Assert.Equal(persistence.ParentCategoryId, category.ParentCategoryId!.Value.Value);
        Assert.Equal("Laptops", category.Name);
    }

    [Fact]
    public void ToDomain_WithEmptyCategoryId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CategoryPersistenceMapper.ToDomain(
            new CategoryPersistence { Name = "Category" }));
    }

    [Fact]
    public void ToDomain_WithEmptyParentId_Throws()
    {
        Assert.Throws<ArgumentException>(() => CategoryPersistenceMapper.ToDomain(
            new CategoryPersistence
            {
                Id = Guid.NewGuid(),
                Name = "Category",
                ParentCategoryId = Guid.Empty
            }));
    }

    [Fact]
    public void ToDomain_WithSelfParenting_Throws()
    {
        var id = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => CategoryPersistenceMapper.ToDomain(
            new CategoryPersistence { Id = id, Name = "Category", ParentCategoryId = id }));
    }

    [Fact]
    public void ToDomain_WithInvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => CategoryPersistenceMapper.ToDomain(
            new CategoryPersistence { Id = Guid.NewGuid(), Name = " " }));
    }
}