using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Persistence.Repositories;

public sealed class ProductRepositoryTests
{
    [Fact]
    public void DeleteQuery_UsesSqlServerProductIdPredicate()
    {
        using var context = CreateContext(new SavingGraphInterceptor());
        var sql = ProductRepository.DeleteQuery(context.Products, ProductId.New()).ToQueryString();
        Assert.Contains("FROM [Products]", sql);
        Assert.Contains("[p].[Id]", sql);
        Assert.Contains("WHERE", sql);
    }

    [Fact]
    public void DeleteQuery_RejectsInvalidArguments()
    {
        using var context = CreateContext(new SavingGraphInterceptor());
        Assert.Throws<ArgumentNullException>(() => ProductRepository.DeleteQuery(null!, ProductId.New()));
        Assert.Throws<ArgumentException>(() => ProductRepository.DeleteQuery(context.Products, default));
    }

    [Fact]
    public void Constructor_RejectsNullContext() =>
        Assert.Throws<ArgumentNullException>(() => new ProductRepository(null!));

    [Fact]
    public async Task AddAsync_PersistsCompleteGraphWithInitialRevision()
    {
        var capture = new SavingGraphInterceptor();
        await using var context = CreateContext(capture);
        var repository = new ProductRepository(context);
        var product = CompleteProduct();

        var token = await repository.AddAsync(product, CancellationToken.None);

        Assert.Equal(product.Id, token.ProductId);
        Assert.NotEqual(Guid.Empty, token.Revision);
        Assert.NotNull(capture.Product);
        Assert.Equal(token.Revision, capture.Product.Version);
        Assert.Equal(product.Id.Value, capture.Product.Id);
        Assert.Equal(product.ProductTypeId.Value, capture.Product.ProductTypeId);
        Assert.Equal(product.Name, capture.Product.Name);
        Assert.Equal(2, capture.Product.Variants.Count);
        Assert.Single(capture.Product.Categories);
        Assert.Single(capture.Product.AttributeValues);
        Assert.Single(capture.Product.AttributeValues.Single().MultiChoiceValues);
        Assert.Single(capture.Product.Variants.First().AttributeValues);
        Assert.Single(capture.Product.Variants.First().AttributeValues.Single().MultiChoiceValues);
    }

    [Fact]
    public async Task AddAsync_RejectsNullAndPropagatesCancellation()
    {
        await using var context = CreateContext(new SavingGraphInterceptor());
        var repository = new ProductRepository(context);

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.AddAsync(null!, CancellationToken.None));

        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            repository.AddAsync(CompleteProduct(), cancellation.Token));
    }

    [Fact]
    public async Task SaveAsync_RejectsInvalidArgumentsBeforeDatabaseAccess()
    {
        await using var context = CreateContext(new SavingGraphInterceptor());
        var repository = new ProductRepository(context);
        var product = CompleteProduct();

        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(null!, null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            repository.SaveAsync(product, null!, CancellationToken.None));

        var foreignToken = ProductConcurrencyToken.Create(
            ProductId.New(), Guid.NewGuid());
        await Assert.ThrowsAsync<ArgumentException>(() =>
            repository.SaveAsync(product, foreignToken, CancellationToken.None));
    }

    [Fact]
    public async Task SaveTrackedAsync_UsesExpectedRevisionAndReturnsReplacementToken()
    {
        var capture = new SavingGraphInterceptor();
        await using var context = CreateContext(capture);
        var repository = new ProductRepository(context);
        var product = CompleteProduct();
        var originalRevision = Guid.NewGuid();
        var persistence = Persisted(product, originalRevision);
        context.Attach(persistence);

        var token = await repository.SaveTrackedAsync(
            product,
            ProductConcurrencyToken.Create(product.Id, originalRevision),
            persistence,
            CancellationToken.None);

        Assert.Equal(product.Id, token.ProductId);
        Assert.NotEqual(Guid.Empty, token.Revision);
        Assert.NotEqual(originalRevision, token.Revision);
        Assert.Equal(originalRevision, capture.OriginalRevision);
        Assert.Equal(token.Revision, capture.CurrentRevision);
    }

    [Fact]
    public async Task SaveTrackedAsync_TranslatesEfConcurrencyConflict()
    {
        await using var context = CreateContext(new ConcurrencyConflictInterceptor());
        var repository = new ProductRepository(context);
        var product = CompleteProduct();
        var originalRevision = Guid.NewGuid();
        var persistence = Persisted(product, originalRevision);
        context.Attach(persistence);

        var exception = await Assert.ThrowsAsync<ProductConcurrencyException>(() =>
            repository.SaveTrackedAsync(
                product,
                ProductConcurrencyToken.Create(product.Id, originalRevision),
                persistence,
                CancellationToken.None));

        Assert.Equal(product.Id, exception.ProductId);
    }

    [Fact]
    public void CompleteGraph_UsesSqlServerSplitQueryWithEveryRequiredNavigation()
    {
        using var context = CreateContext(new SavingGraphInterceptor());

        var query = ProductRepository.CompleteGraph(context.Set<ProductPersistence>());
        var expression = query.Expression.ToString();

        Assert.Contains(nameof(ProductPersistence.Variants), expression);
        Assert.Contains(nameof(ProductPersistence.Categories), expression);
        Assert.Contains(nameof(ProductPersistence.AttributeValues), expression);
        Assert.Contains(nameof(ProductVariantPersistence.AttributeValues), expression);
        Assert.Contains(nameof(ProductAttributeValuePersistence.MultiChoiceValues), expression);
        Assert.Contains(nameof(ProductVariantAttributeValuePersistence.MultiChoiceValues), expression);
        Assert.Contains("AsSplitQuery", expression);
        Assert.Contains("FROM [Products]", query.ToQueryString());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void CompleteGraph_LoadsVariantPriceRulesInTrackedAndUntrackedQueries(bool noTracking)
    {
        using var context = CreateContext(new SavingGraphInterceptor());
        var query = ProductRepository.CompleteGraph(context.Products);
        if (noTracking)
            query = query.AsNoTracking();

        // A single query exposes all included tables without executing a database command.
        // Production keeps its split-query strategy.
        var sql = query.AsSingleQuery().ToQueryString();

        Assert.Contains("[PriceRules]", sql);
        Assert.Contains("[ProductVariantId]", sql);
        Assert.Contains("[AdjustmentType]", sql);
        Assert.Contains("[StartsAt]", sql);
        Assert.Contains("[EndsAt]", sql);
    }

    [Fact]
    public async Task SaveTrackedAsync_PreservesRetainedPriceRuleAndDeletesRemovedRule()
    {
        var capture = new SavingGraphInterceptor();
        await using var context = CreateContext(capture);
        var repository = new ProductRepository(context);
        var product = CompleteProduct();
        var variant = product.Variants.First();
        product.SetVariantPrice(variant.Id, Money.Create(100m, "EUR"));
        var retained = PriceRule.Create("Retained", PriceAdjustmentType.PercentageDiscount, 10m, 1);
        var removed = PriceRule.Create("Removed", PriceAdjustmentType.PercentageDiscount, 20m, 2);
        product.AddVariantPriceRule(variant.Id, retained);
        product.AddVariantPriceRule(variant.Id, removed);
        var revision = Guid.NewGuid();
        var persistence = Persisted(product, revision);
        context.Attach(persistence);
        var persistedVariant = persistence.Variants.Single(row => row.Id == variant.Id.Value);
        var retainedRow = persistedVariant.PriceRules.Single(row => row.Id == retained.Id);
        var removedRow = persistedVariant.PriceRules.Single(row => row.Id == removed.Id);
        var snapshot = MyShop.Infrastructure.Persistence.Mappers.ProductPersistenceMapper.ToSnapshot(persistence);
        var rehydratedVariant = snapshot.Product.Variants.Single(row => row.Id == variant.Id);
        Assert.Equal(2, rehydratedVariant.PriceRules.Count);
        snapshot.Product.RemoveVariantPriceRule(variant.Id, removed.Id);
        snapshot.Product.SetVariantPrice(variant.Id, Money.Create(200m, "EUR"));

        await repository.SaveTrackedAsync(snapshot.Product, snapshot.ConcurrencyToken, persistence, CancellationToken.None);

        Assert.Same(retainedRow, Assert.Single(persistedVariant.PriceRules));
        Assert.Equal(EntityState.Unchanged, context.Entry(retainedRow).State);
        Assert.Equal(EntityState.Deleted, context.Entry(removedRow).State);
        Assert.Equal(200m, persistedVariant.PriceAmount);
        Assert.Equal("EUR", persistedVariant.PriceCurrency);
        Assert.Equal(Money.Create(180m, "EUR"), rehydratedVariant.CalculatePrice(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public async Task SaveTrackedAsync_AddsPriceRuleAndClearsBasePriceWithoutReplacingRetainedRule()
    {
        await using var context = CreateContext(new SavingGraphInterceptor());
        var product = CompleteProduct();
        var variant = product.Variants.First();
        product.SetVariantPrice(variant.Id, Money.Create(20m, "EUR"));
        var retained = PriceRule.Create("Existing", PriceAdjustmentType.FixedDiscount, 1m, 0);
        product.AddVariantPriceRule(variant.Id, retained);
        var persistence = Persisted(product, Guid.NewGuid());
        context.Attach(persistence);
        var row = persistence.Variants.Single(candidate => candidate.Id == variant.Id.Value);
        var retainedRow = Assert.Single(row.PriceRules);
        var snapshot = MyShop.Infrastructure.Persistence.Mappers.ProductPersistenceMapper.ToSnapshot(persistence);
        var added = PriceRule.Create("New", PriceAdjustmentType.PercentageDiscount, 25m, 5,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        snapshot.Product.AddVariantPriceRule(variant.Id, added);
        snapshot.Product.ClearVariantPrice(variant.Id);

        await new ProductRepository(context).SaveTrackedAsync(
            snapshot.Product, snapshot.ConcurrencyToken, persistence, CancellationToken.None);

        Assert.Null(row.PriceAmount);
        Assert.Null(row.PriceCurrency);
        Assert.Equal(2, row.PriceRules.Count);
        Assert.Same(retainedRow, row.PriceRules.Single(rule => rule.Id == retained.Id));
        Assert.Equal(EntityState.Unchanged, context.Entry(retainedRow).State);
        var addedRow = row.PriceRules.Single(rule => rule.Id == added.Id);
        Assert.Equal(EntityState.Added, context.Entry(addedRow).State);
        Assert.Equal(row.Id, addedRow.ProductVariantId);
        Assert.Same(row, addedRow.ProductVariant);
        Assert.Equal("New", addedRow.Name);
        Assert.Equal(25m, addedRow.Value);
        Assert.Equal(5, addedRow.Priority);
        Assert.Equal(added.StartsAt, addedRow.StartsAt);
        Assert.Equal((int)PriceAdjustmentType.PercentageDiscount, addedRow.AdjustmentType);
    }

    [Fact]
    public async Task SaveTrackedAsync_UpdatesPriceRuleInPlaceWithExpectedRevision()
    {
        var capture = new SavingGraphInterceptor();
        await using var context = CreateContext(capture);
        var product = CompleteProduct();
        var variant = product.Variants.First();
        var rule = PriceRule.Create("Old", PriceAdjustmentType.FixedDiscount, 1m, 0);
        product.AddVariantPriceRule(variant.Id, rule);
        var revision = Guid.NewGuid();
        var persistence = Persisted(product, revision);
        context.Attach(persistence);
        var variantRow = persistence.Variants.Single(candidate => candidate.Id == variant.Id.Value);
        var ruleRow = Assert.Single(variantRow.PriceRules);
        var snapshot = MyShop.Infrastructure.Persistence.Mappers.ProductPersistenceMapper.ToSnapshot(persistence);
        var startsAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2));
        snapshot.Product.UpdateVariantPriceRule(variant.Id, rule.Id, "Updated",
            PriceAdjustmentType.PercentageDiscount, 25m, 5, startsAt, startsAt.AddDays(1));

        var token = await new ProductRepository(context).SaveTrackedAsync(
            snapshot.Product, snapshot.ConcurrencyToken, persistence, CancellationToken.None);

        Assert.Same(ruleRow, Assert.Single(variantRow.PriceRules));
        Assert.Equal(rule.Id, ruleRow.Id);
        Assert.Equal(variant.Id.Value, ruleRow.ProductVariantId);
        Assert.Same(variantRow, ruleRow.ProductVariant);
        Assert.Equal(EntityState.Modified, context.Entry(ruleRow).State);
        Assert.Equal("Updated", ruleRow.Name);
        Assert.Equal((int)PriceAdjustmentType.PercentageDiscount, ruleRow.AdjustmentType);
        Assert.Equal(25m, ruleRow.Value);
        Assert.Equal(5, ruleRow.Priority);
        Assert.Equal(startsAt, ruleRow.StartsAt);
        Assert.Equal(startsAt.Offset, ruleRow.StartsAt!.Value.Offset);
        Assert.Equal(startsAt.AddDays(1), ruleRow.EndsAt);
        Assert.Equal(revision, capture.OriginalRevision);
        Assert.NotEqual(revision, token.Revision);
        Assert.NotEqual(Guid.Empty, token.Revision);
        Assert.Equal(token.Revision, capture.CurrentRevision);
    }

    private static MyShopDbContext CreateContext(SaveChangesInterceptor interceptor)
    {
        var options = new DbContextOptionsBuilder<MyShopDbContext>()
            .UseSqlServer()
            .AddInterceptors(interceptor)
            .Options;
        return new MyShopDbContext(options);
    }

    private static Product CompleteProduct()
    {
        var product = Product.Create("Product", ProductTypeId.New(), "Primary");
        var primary = product.Variants.Single();
        var secondary = product.AddVariant("Secondary");
        product.SetVariantSku(primary.Id, Sku.Create("sku-primary"));
        product.AssignToCategory(CategoryId.New());
        product.SetAttributeValue(MultiChoiceAttributeValue.Create(
            AttributeDefinitionId.New(), [ChoiceValue.Create("product choice")]));
        product.SetVariantAttributeValue(primary.Id, MultiChoiceAttributeValue.Create(
            AttributeDefinitionId.New(), [ChoiceValue.Create("variant choice")]));
        product.SetVariantAttributeValue(secondary.Id, BooleanAttributeValue.Create(
            AttributeDefinitionId.New(), true));
        return product;
    }

    private static ProductPersistence Persisted(Product product, Guid revision)
    {
        var persistence = new ProductPersistence
        {
            Id = product.Id.Value,
            Version = revision
        };
        MyShop.Infrastructure.Persistence.Mappers.ProductPersistenceSynchronizer.Synchronize(
            product, persistence);
        return persistence;
    }

    private sealed class SavingGraphInterceptor : SaveChangesInterceptor
    {
        public ProductPersistence? Product { get; private set; }
        public Guid? OriginalRevision { get; private set; }
        public Guid? CurrentRevision { get; private set; }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Product = eventData.Context!.ChangeTracker.Entries<ProductPersistence>()
                .Select(entry => entry.Entity)
                .Single();
            var version = eventData.Context.Entry(Product).Property(product => product.Version);
            OriginalRevision = version.OriginalValue;
            CurrentRevision = version.CurrentValue;
            return ValueTask.FromResult(InterceptionResult<int>.SuppressWithResult(1));
        }
    }

    private sealed class ConcurrencyConflictInterceptor : SaveChangesInterceptor
    {
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default) =>
            ValueTask.FromException<InterceptionResult<int>>(new DbUpdateConcurrencyException());
    }
}
