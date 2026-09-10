using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductTypeRehydrationTests
{
    [Fact]
    public void Rehydrate_PreservesAllStateAndOrder()
    {
        var productTypeId = ProductTypeId.New();
        var first = AttributeDefinition.Rehydrate(
            AttributeDefinitionId.New(), AttributeCode.Create("first"), "First",
            AttributeDataType.Text, true, false, AttributeScope.Product);
        var second = AttributeDefinition.Rehydrate(
            AttributeDefinitionId.New(), AttributeCode.Create("second"), "Second",
            AttributeDataType.Integer, false, true, AttributeScope.Variant);

        var productType = ProductType.Rehydrate(productTypeId, "Type", [first, second]);

        Assert.Equal(productTypeId, productType.Id);
        Assert.Equal("Type", productType.Name);
        Assert.Equal([first, second], productType.AttributeDefinitions);
        Assert.Equal(first.Id, productType.AttributeDefinitions.First().Id);
        Assert.True(first.IsRequired);
        Assert.False(first.IsFilterable);
        Assert.Equal(AttributeScope.Product, first.Scope);
        Assert.Equal(AttributeDataType.Integer, second.DataType);
    }

    [Fact]
    public void Rehydrate_AllowsEmptyDefinitions()
    {
        var productType = ProductType.Rehydrate(ProductTypeId.New(), "Type", []);

        Assert.Empty(productType.AttributeDefinitions);
    }

    [Fact]
    public void Rehydrate_RejectsDuplicateDefinitionIdsAndCodes()
    {
        var definitionId = AttributeDefinitionId.New();
        var first = AttributeDefinition.Rehydrate(
            definitionId, AttributeCode.Create("first"), "First",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        var duplicateId = AttributeDefinition.Rehydrate(
            definitionId, AttributeCode.Create("second"), "Second",
            AttributeDataType.Text, false, false, AttributeScope.Product);
        var duplicateCode = AttributeDefinition.Rehydrate(
            AttributeDefinitionId.New(), AttributeCode.Create("first"), "Other",
            AttributeDataType.Text, false, false, AttributeScope.Product);

        Assert.Throws<InvalidOperationException>(() => ProductType.Rehydrate(
            ProductTypeId.New(), "Type", [first, duplicateId]));
        Assert.Throws<InvalidOperationException>(() => ProductType.Rehydrate(
            ProductTypeId.New(), "Type", [first, duplicateCode]));
    }

    [Fact]
    public void Rehydrate_RejectsNullEntriesAndInvalidInputs()
    {
        Assert.Throws<ArgumentNullException>(() => ProductType.Rehydrate(
            ProductTypeId.New(), "Type", null!));
        Assert.Throws<InvalidOperationException>(() => ProductType.Rehydrate(
            ProductTypeId.New(), "Type", new AttributeDefinition[] { null! }));
        Assert.Throws<ArgumentException>(() => ProductType.Rehydrate(
            default, "Type", []));
        Assert.Throws<ArgumentException>(() => ProductType.Rehydrate(
            ProductTypeId.New(), " ", []));
    }
}

public sealed class AttributeDefinitionRehydrationTests
{
    [Fact]
    public void Rehydrate_PreservesAllState()
    {
        var id = AttributeDefinitionId.New();
        var code = AttributeCode.Create("colour");

        var definition = AttributeDefinition.Rehydrate(
            id, code, "Colour", AttributeDataType.Choice, true, true, AttributeScope.Variant);

        Assert.Equal(id, definition.Id);
        Assert.Equal(code, definition.Code);
        Assert.Equal("Colour", definition.DisplayName);
        Assert.Equal(AttributeDataType.Choice, definition.DataType);
        Assert.True(definition.IsRequired);
        Assert.True(definition.IsFilterable);
        Assert.Equal(AttributeScope.Variant, definition.Scope);
    }

    [Fact]
    public void Rehydrate_UsesExistingValidation()
    {
        Assert.Throws<ArgumentException>(() => AttributeDefinition.Rehydrate(
            default, AttributeCode.Create("colour"), "Colour",
            AttributeDataType.Text, false, false, AttributeScope.Product));
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeDefinition.Rehydrate(
            AttributeDefinitionId.New(), AttributeCode.Create("colour"), "Colour",
            (AttributeDataType)999, false, false, AttributeScope.Product));
        Assert.Throws<ArgumentOutOfRangeException>(() => AttributeDefinition.Rehydrate(
            AttributeDefinitionId.New(), AttributeCode.Create("colour"), "Colour",
            AttributeDataType.Text, false, false, (AttributeScope)999));
    }
}
