using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class ProductTypePersistenceMapperTests
{
    [Fact]
    public void ToDomain_WithNullPersistence_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ProductTypePersistenceMapper.ToDomain(null!));
    }

    [Fact]
    public void ToDomain_PreservesProductTypeAndAttributeDefinitionStateInOrder()
    {
        var productTypeId = Guid.NewGuid();
        var firstId = Guid.NewGuid();
        var secondId = Guid.NewGuid();
        var persistence = new ProductTypePersistence
        {
            Id = productTypeId,
            Name = "Vehicle",
            AttributeDefinitions =
            [
                new AttributeDefinitionPersistence
                {
                    Id = firstId,
                    Code = "make",
                    DisplayName = "Make",
                    DataType = AttributeDataType.Text,
                    Scope = AttributeScope.Product,
                    IsRequired = true,
                    IsFilterable = true
                },
                new AttributeDefinitionPersistence
                {
                    Id = secondId,
                    Code = "colour",
                    DisplayName = "Colour",
                    DataType = AttributeDataType.Choice,
                    Scope = AttributeScope.Variant,
                    IsRequired = false,
                    IsFilterable = true
                }
            ]
        };

        var productType = ProductTypePersistenceMapper.ToDomain(persistence);

        Assert.Equal(productTypeId, productType.Id.Value);
        Assert.Equal("Vehicle", productType.Name);
        var definitions = productType.AttributeDefinitions.ToArray();
        Assert.Equal([firstId, secondId], definitions.Select(definition => definition.Id.Value));
        Assert.Equal("make", definitions[0].Code.Value);
        Assert.Equal("Make", definitions[0].DisplayName);
        Assert.Equal(AttributeDataType.Text, definitions[0].DataType);
        Assert.Equal(AttributeScope.Product, definitions[0].Scope);
        Assert.True(definitions[0].IsRequired);
        Assert.True(definitions[0].IsFilterable);
        Assert.Equal("colour", definitions[1].Code.Value);
        Assert.Equal("Colour", definitions[1].DisplayName);
        Assert.Equal(AttributeDataType.Choice, definitions[1].DataType);
        Assert.Equal(AttributeScope.Variant, definitions[1].Scope);
        Assert.False(definitions[1].IsRequired);
        Assert.True(definitions[1].IsFilterable);
    }

    [Fact]
    public void ToDomain_WithEmptyDefinitions_Succeeds()
    {
        var productType = ProductTypePersistenceMapper.ToDomain(new ProductTypePersistence
        {
            Id = Guid.NewGuid(),
            Name = "Vehicle",
            AttributeDefinitions = []
        });

        Assert.Empty(productType.AttributeDefinitions);
    }

    [Theory]
    [InlineData("", "Valid name")]
    [InlineData("invalid-code", "Valid name")]
    [InlineData("make", "")]
    public void ToDomain_InvalidPersistedValues_Throw(string code, string displayName)
    {
        var persistence = CreateProductType();
        persistence.AttributeDefinitions.Add(new AttributeDefinitionPersistence
        {
            Id = Guid.NewGuid(),
            Code = code,
            DisplayName = displayName,
            DataType = AttributeDataType.Text,
            Scope = AttributeScope.Product
        });

        Assert.Throws<ArgumentException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    [Fact]
    public void ToDomain_WithEmptyProductTypeId_Throws()
    {
        var persistence = CreateProductType();
        persistence.Id = Guid.Empty;

        Assert.Throws<ArgumentException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    [Fact]
    public void ToDomain_WithInvalidProductTypeName_Throws()
    {
        var persistence = CreateProductType();
        persistence.Name = " ";

        Assert.Throws<ArgumentException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    [Fact]
    public void ToDomain_WithEmptyAttributeDefinitionId_Throws()
    {
        var persistence = CreateProductType();
        persistence.AttributeDefinitions.Add(new AttributeDefinitionPersistence
        {
            Code = "make",
            DisplayName = "Make",
            DataType = AttributeDataType.Text,
            Scope = AttributeScope.Product
        });

        Assert.Throws<ArgumentException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    [Fact]
    public void ToDomain_WithDuplicateDefinitionIds_Throws()
    {
        var persistence = CreateProductType();
        var id = Guid.NewGuid();
        persistence.AttributeDefinitions.Add(CreateDefinition(id, "make"));
        persistence.AttributeDefinitions.Add(CreateDefinition(id, "model"));

        Assert.Throws<InvalidOperationException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    [Fact]
    public void ToDomain_WithDuplicateCodes_Throws()
    {
        var persistence = CreateProductType();
        persistence.AttributeDefinitions.Add(CreateDefinition(Guid.NewGuid(), "make"));
        persistence.AttributeDefinitions.Add(CreateDefinition(Guid.NewGuid(), "make"));

        Assert.Throws<InvalidOperationException>(() => ProductTypePersistenceMapper.ToDomain(persistence));
    }

    private static ProductTypePersistence CreateProductType() => new()
    {
        Id = Guid.NewGuid(),
        Name = "Vehicle",
        AttributeDefinitions = []
    };

    private static AttributeDefinitionPersistence CreateDefinition(Guid id, string code) => new()
    {
        Id = id,
        Code = code,
        DisplayName = code,
        DataType = AttributeDataType.Text,
        Scope = AttributeScope.Product
    };
}