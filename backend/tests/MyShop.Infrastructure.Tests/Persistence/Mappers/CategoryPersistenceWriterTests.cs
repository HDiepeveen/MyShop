using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class CategoryPersistenceWriterTests
{
    [Fact]
    public void Write_UpdatesRootState()
    {
        var category = Category.CreateRoot("Clothing");
        var persistence = new CategoryPersistence
        {
            Id = category.Id.Value,
            Name = "Old",
            ParentCategoryId = Guid.NewGuid()
        };
        CategoryPersistenceWriter.Write(category, persistence);
        Assert.Equal("Clothing", persistence.Name);
        Assert.Null(persistence.ParentCategoryId);
    }

    [Fact]
    public void Write_UpdatesChildState()
    {
        var parentId = CategoryId.New();
        var category = Category.CreateChild("Shirts", parentId);
        var persistence = new CategoryPersistence { Id = category.Id.Value };
        CategoryPersistenceWriter.Write(category, persistence);
        Assert.Equal("Shirts", persistence.Name);
        Assert.Equal(parentId.Value, persistence.ParentCategoryId);
    }

    [Fact]
    public void Write_RejectsInvalidIdentityBeforeMutation()
    {
        var category = Category.CreateRoot("Clothing");
        var persistence = new CategoryPersistence { Id = Guid.NewGuid(), Name = "Original" };
        Assert.Throws<ArgumentException>(() => CategoryPersistenceWriter.Write(category, persistence));
        Assert.Equal("Original", persistence.Name);
        persistence.Id = Guid.Empty;
        Assert.Throws<InvalidOperationException>(() => CategoryPersistenceWriter.Write(category, persistence));
    }
}
