using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValidation.GetProductAttributeValidation;

namespace MyShop.Infrastructure.Tests.Integration;

[Collection(SqlServerCollection.Name)]
public sealed class SqlServerAttributeValidationTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task ReadsCurrentRequiredConfigurationAndPersistedRemovalWithoutWriting()
    {
        var data = await SqlServerProductData.Seed(database);
        var definition = data.Definitions[(AttributeScope.Product, AttributeDataType.Boolean)];
        await using (var context = database.CreateContext())
        {
            data.Type.SetAttributeRequired(definition, true);
            await new ProductTypeRepository(context).SaveAsync(data.Type, default);
        }
        await using (var context = database.CreateContext())
        {
            var result = await new UseCase(new ProductRepository(context), new ProductTypeRepository(context))
                .ExecuteAsync(new(data.Product.Id), default);
            Assert.True(result.IsSuccess);
            Assert.Empty(result.Issues!); // A persisted false boolean is a supplied value.
            Assert.Empty(context.ChangeTracker.Entries());
        }
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            snapshot.Product.RemoveAttributeValue(definition);
            await repository.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, default);
        }
        await using (var context = database.CreateContext())
        {
            var repository = new ProductRepository(context);
            var before = (await repository.GetByIdAsync(data.Product.Id, default))!;
            var result = await new UseCase(repository, new ProductTypeRepository(context)).ExecuteAsync(new(data.Product.Id), default);
            Assert.Equal(new ProductAttributeIssue(definition, null, ProductAttributeIssueCode.MissingRequired), Assert.Single(result.Issues!));
            var after = (await repository.GetByIdAsync(data.Product.Id, default))!;
            Assert.Equal(before.ConcurrencyToken, after.ConcurrencyToken);
            Assert.Empty(context.ChangeTracker.Entries());
        }
    }

    [SqlServerFact]
    public async Task MissingVariantRequirementDisappearsAfterSavingThatVariant()
    {
        var data = await SqlServerProductData.Seed(database);
        var definition = data.Definitions[(AttributeScope.Variant, AttributeDataType.Text)];
        await using (var context = database.CreateContext())
        {
            data.Type.SetAttributeRequired(definition, true);
            await new ProductTypeRepository(context).SaveAsync(data.Type, default);
        }
        await using (var context = database.CreateContext())
        {
            var result = await new UseCase(new ProductRepository(context), new ProductTypeRepository(context))
                .ExecuteAsync(new(data.Product.Id), default);
            Assert.Equal(new ProductAttributeIssue(definition, data.Other.Id, ProductAttributeIssueCode.MissingRequired), Assert.Single(result.Issues!));
            var repository = new ProductRepository(context);
            var snapshot = (await repository.GetByIdAsync(data.Product.Id, default))!;
            snapshot.Product.SetVariantAttributeValue(data.Other.Id, TextAttributeValue.Create(definition, "Completed"));
            await repository.SaveAsync(snapshot.Product, snapshot.ConcurrencyToken, default);
        }
        await using (var context = database.CreateContext())
        {
            var result = await new UseCase(new ProductRepository(context), new ProductTypeRepository(context))
                .ExecuteAsync(new(data.Product.Id), default);
            Assert.Empty(result.Issues!);
        }
    }

    [SqlServerFact]
    public async Task RemovedDefinitionLeavesStoredValueVisibleAsUnknown()
    {
        var data = await SqlServerProductData.Seed(database);
        var definition = data.Definitions[(AttributeScope.Product, AttributeDataType.MultiChoice)];
        await using (var context = database.CreateContext())
        {
            data.Type.RemoveAttribute(definition);
            await new ProductTypeRepository(context).SaveAsync(data.Type, default);
        }
        await using var verification = database.CreateContext();
        var repository = new ProductRepository(verification);
        var result = await new UseCase(repository, new ProductTypeRepository(verification)).ExecuteAsync(new(data.Product.Id), default);
        Assert.Equal(new ProductAttributeIssue(definition, null, ProductAttributeIssueCode.UnknownDefinition), Assert.Single(result.Issues!));
        var product = (await repository.GetByIdAsync(data.Product.Id, default))!;
        Assert.Contains(product.Product.AttributeValues, value => value.AttributeDefinitionId == definition);
        Assert.Equal(data.Token, product.ConcurrencyToken);
        Assert.Empty(verification.ChangeTracker.Entries());
    }
}
