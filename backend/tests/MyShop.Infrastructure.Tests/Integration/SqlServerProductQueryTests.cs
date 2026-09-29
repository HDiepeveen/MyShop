using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerProductQueryTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task FiltersApplyBeforePagingAndCountsRemainAvailableBeyondLastPage()
    {
        var data = await SqlServerProductData.Seed(database);
        var prefix = "Page-" + Guid.NewGuid().ToString("N");
        var matching = new List<Product>();
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            foreach (var suffix in new[] { "C", "A", "B" })
            {
                var product = Product.Create(prefix + suffix, data.Type.Id, "First");
                product.AddVariant("Second");
                product.AssignToCategory(data.Category.Id);
                await repository.AddAsync(product, default);
                matching.Add(product);
            }
            var withoutCategory = Product.Create(prefix + "D", data.Type.Id, "First");
            await repository.AddAsync(withoutCategory, default);
        }
        await using var verification = database.CreateContext();
        var queries = new ProductListRepository(verification);
        var page = await queries.ListAsync(1, 1, data.Type.Id, data.Category.Id, prefix, default);
        Assert.Equal(3, page.TotalCount);
        var item = Assert.Single(page.Items);
        Assert.Equal(matching.Single(product => product.Name == prefix + "B").Id.Value, item.Id);
        Assert.Equal(2, item.VariantCount);
        var beyond = await queries.ListAsync(10, 1, data.Type.Id, data.Category.Id, prefix, default);
        Assert.Equal(3, beyond.TotalCount);
        Assert.Empty(beyond.Items);
        Assert.Equal(4, (await queries.ListAsync(0, 10, data.Type.Id, null, prefix, default)).TotalCount);
        Assert.Equal(0, (await queries.ListAsync(0, 10, ProductTypeId.New(), data.Category.Id, prefix, default)).TotalCount);
        Assert.Empty(verification.ChangeTracker.Entries());
    }

    [SqlServerFact]
    public async Task DuplicateSkuIsRejectedByUniqueIndexAndEntireInsertIsRolledBack()
    {
        var data = await SqlServerProductData.Seed(database);
        var duplicate = Product.Create("Duplicate SKU", data.Type.Id, "First");
        duplicate.SetVariantSku(duplicate.Variants.Single().Id, data.Variant.Sku!);
        await using (var context = database.CreateContext())
        {
            var error = await Assert.ThrowsAsync<DbUpdateException>(() =>
                new ProductRepository(context).AddAsync(duplicate, default));
            var sqlError = Assert.IsType<SqlException>(error.InnerException);
            Assert.Contains(sqlError.Number, new[] { 2601, 2627 });
        }
        await using var verification = database.CreateContext();
        Assert.False(await verification.Products.AnyAsync(row => row.Id == duplicate.Id.Value));
        Assert.False(await verification.ProductVariants.AnyAsync(row => row.ProductId == duplicate.Id.Value));
        var owner = await new ProductSkuLookup(verification).FindOwnerAsync(data.Variant.Sku!, default);
        Assert.Equal(data.Variant.Id, owner!.ProductVariantId);
    }
}
