using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerCategoryTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task HierarchyAndRenamePersistAcrossContexts()
    {
        var root = Category.CreateRoot("Root");
        var child = Category.CreateChild("Child", root.Id);
        await using (var context = database.CreateContext())
        {
            var repository = new CategoryRepository(context);
            await repository.AddAsync(root, default);
            await repository.AddAsync(child, default);
        }
        await using (var context = database.CreateContext())
        {
            var repository = new CategoryRepository(context);
            Assert.True(await repository.IsDescendantOfAsync(child.Id, root.Id, default));
            Assert.Equal(1, (await repository.GetUsageAsync(root.Id, default)).DirectChildCount);
            var loaded = await repository.GetByIdAsync(child.Id, default);
            Assert.NotNull(loaded);
            loaded.Rename("Renamed");
            loaded.MoveToRoot();
            await repository.SaveAsync(loaded, default);
        }
        await using (var context = database.CreateContext())
        {
            var repository = new CategoryRepository(context);
            var loaded = await repository.GetByIdAsync(child.Id, default);
            Assert.Equal("Renamed", loaded!.Name);
            Assert.True(loaded.IsRoot);
            Assert.False(await repository.IsDescendantOfAsync(child.Id, root.Id, default));
            Assert.Equal(0, (await repository.GetUsageAsync(root.Id, default)).DirectChildCount);
        }
    }

    [SqlServerFact]
    public async Task DatabaseRejectsDeletingAParentBeforeItsChild()
    {
        var root = Category.CreateRoot("Protected root");
        var child = Category.CreateChild("Child", root.Id);
        await using (var context = database.CreateContext())
        {
            var repository = new CategoryRepository(context);
            await repository.AddAsync(root, default);
            await repository.AddAsync(child, default);
        }
        await using (var context = database.CreateContext())
            await Assert.ThrowsAsync<DbUpdateException>(() => new CategoryRepository(context).DeleteAsync(root.Id, default));
        await using (var context = database.CreateContext())
        {
            var repository = new CategoryRepository(context);
            Assert.NotNull(await repository.GetByIdAsync(root.Id, default));
            await repository.DeleteAsync(child.Id, default);
            await repository.DeleteAsync(root.Id, default);
        }
        await using (var context = database.CreateContext())
            Assert.Null(await new CategoryRepository(context).GetByIdAsync(root.Id, default));
    }
}
