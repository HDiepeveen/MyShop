using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductTypePersistenceSynchronizerTests
{
    [Fact]
    public void Synchronize_UpdatesCompleteGraph()
    {
        var productType = ProductType.Create("Vehicle");
        var retainedId = AttributeDefinitionId.New();
        productType.AddAttribute(
            retainedId, AttributeCode.Create("make"), "Make", AttributeDataType.Text,
            true, true, AttributeScope.Product);
        var added = productType.AddAttribute(
            AttributeDefinitionId.New(), AttributeCode.Create("colour"), "Colour",
            AttributeDataType.Choice, false, true, AttributeScope.Variant);
        var persistence = new ProductTypePersistence
        {
            Id = productType.Id.Value,
            Name = "Old name"
        };
        var retained = CreateDefinition(persistence, retainedId.Value, "old");
        var removed = CreateDefinition(persistence, Guid.NewGuid(), "removed");
        persistence.AttributeDefinitions.Add(retained);
        persistence.AttributeDefinitions.Add(removed);

        ProductTypePersistenceSynchronizer.Synchronize(productType, persistence);

        Assert.Equal("Vehicle", persistence.Name);
        Assert.DoesNotContain(removed, persistence.AttributeDefinitions);
        Assert.Same(retained, persistence.AttributeDefinitions.Single(x => x.Id == retainedId.Value));
        Assert.Equal("make", retained.Code);
        Assert.Equal("Make", retained.DisplayName);
        Assert.True(retained.IsRequired);
        Assert.True(retained.IsFilterable);
        var addedPersistence = persistence.AttributeDefinitions.Single(x => x.Id == added.Id.Value);
        Assert.Equal(persistence.Id, addedPersistence.ProductTypeId);
        Assert.Same(persistence, addedPersistence.ProductType);
        Assert.Equal("colour", addedPersistence.Code);
        Assert.Equal(AttributeDataType.Choice, addedPersistence.DataType);
        Assert.Equal(AttributeScope.Variant, addedPersistence.Scope);
    }

    [Fact]
    public void Synchronize_RejectsInvalidArgumentsAndStructureBeforeMutation()
    {
        var productType = ProductType.Create("Vehicle");
        var persistence = new ProductTypePersistence
        {
            Id = productType.Id.Value,
            Name = "Original"
        };

        Assert.Throws<ArgumentNullException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(null!, persistence));
        Assert.Throws<ArgumentNullException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(productType, null!));

        persistence.Id = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(productType, persistence));
        Assert.Equal("Original", persistence.Name);

        persistence.Id = Guid.Empty;
        Assert.Throws<InvalidOperationException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(productType, persistence));
    }

    [Fact]
    public void Synchronize_RejectsDuplicatePersistedDefinitionsBeforeMutation()
    {
        var productType = ProductType.Create("Vehicle");
        var persistence = new ProductTypePersistence
        {
            Id = productType.Id.Value,
            Name = "Original"
        };
        var id = Guid.NewGuid();
        persistence.AttributeDefinitions.Add(CreateDefinition(persistence, id, "first"));
        persistence.AttributeDefinitions.Add(CreateDefinition(persistence, id, "second"));

        Assert.Throws<InvalidOperationException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(productType, persistence));
        Assert.Equal("Original", persistence.Name);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Synchronize_RejectsInvalidPersistedDefinitionIdentityBeforeMutation(bool emptyId)
    {
        var productType = ProductType.Create("Vehicle");
        var persistence = new ProductTypePersistence
        {
            Id = productType.Id.Value,
            Name = "Original"
        };
        var definition = CreateDefinition(
            persistence, emptyId ? Guid.Empty : Guid.NewGuid(), "make");
        if (!emptyId)
            definition.ProductTypeId = Guid.NewGuid();
        persistence.AttributeDefinitions.Add(definition);

        Assert.Throws<InvalidOperationException>(() =>
            ProductTypePersistenceSynchronizer.Synchronize(productType, persistence));
        Assert.Equal("Original", persistence.Name);
    }

    private static AttributeDefinitionPersistence CreateDefinition(
        ProductTypePersistence owner,
        Guid id,
        string code) => new()
    {
        Id = id,
        ProductTypeId = owner.Id,
        ProductType = owner,
        Code = code,
        DisplayName = code,
        DataType = AttributeDataType.Text,
        Scope = AttributeScope.Product
    };
}
