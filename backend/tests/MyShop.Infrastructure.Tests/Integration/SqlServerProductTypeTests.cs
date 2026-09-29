using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProductTypeTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task AttributeEditsAdditionsAndRemovalsPersistAcrossContexts()
    {
        var type = ProductType.Create("Type");
        var removed = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("old"), "Old",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        var retained = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"), "Color",
            AttributeDataType.Choice, false, false, AttributeScope.Variant);
        var addedId = AttributeDefinitionId.New();
        await using (var context = database.CreateContext())
            await new ProductTypeRepository(context).AddAsync(type, default);
        await using (var context = database.CreateContext())
        {
            var repository = new ProductTypeRepository(context);
            var loaded = (await repository.GetByIdAsync(type.Id, default))!;
            loaded.Rename("Updated type");
            loaded.RemoveAttribute(removed.Id);
            loaded.RenameAttribute(retained.Id, "Colour");
            loaded.SetAttributeRequired(retained.Id, true);
            loaded.SetAttributeFilterable(retained.Id, true);
            loaded.AddAttribute(addedId, AttributeCode.Create("size"), "Size",
                AttributeDataType.Integer, true, false, AttributeScope.Variant);
            await repository.SaveAsync(loaded, default);
        }
        await using (var context = database.CreateContext())
        {
            var loaded = (await new ProductTypeRepository(context).GetByIdAsync(type.Id, default))!;
            Assert.Equal("Updated type", loaded.Name);
            Assert.Equal(2, loaded.AttributeDefinitions.Count);
            Assert.DoesNotContain(loaded.AttributeDefinitions, attribute => attribute.Id == removed.Id);
            var color = Assert.Single(loaded.AttributeDefinitions, attribute => attribute.Id == retained.Id);
            Assert.Equal("Colour", color.DisplayName);
            Assert.True(color.IsRequired);
            Assert.True(color.IsFilterable);
            Assert.Equal(AttributeScope.Variant, color.Scope);
            Assert.Contains(loaded.AttributeDefinitions, attribute => attribute.Id == addedId);
            Assert.False(await context.AttributeDefinitions.AnyAsync(row => row.Id == removed.Id.Value));
        }
    }

    [SqlServerFact]
    public async Task DeletingUnusedTypeCascadesOnlyItsDefinitions()
    {
        var type = ProductType.Create("Disposable");
        var attribute = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("value"), "Value",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        var other = ProductType.Create("Retained");
        await using (var context = database.CreateContext())
        {
            var repository = new ProductTypeRepository(context);
            await repository.AddAsync(type, default);
            await repository.AddAsync(other, default);
        }
        await using (var context = database.CreateContext())
            await new ProductTypeRepository(context).DeleteAsync(type.Id, default);
        await using (var context = database.CreateContext())
        {
            Assert.Null(await new ProductTypeRepository(context).GetByIdAsync(type.Id, default));
            Assert.False(await context.AttributeDefinitions.AnyAsync(row => row.Id == attribute.Id.Value));
            Assert.NotNull(await new ProductTypeRepository(context).GetByIdAsync(other.Id, default));
        }
    }
}
