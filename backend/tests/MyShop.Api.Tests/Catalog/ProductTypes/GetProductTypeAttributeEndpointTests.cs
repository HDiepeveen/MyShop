using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeAttribute.GetProductTypeAttribute;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class GetProductTypeAttributeEndpointTests
{
    [Theory]
    [InlineData(AttributeDataType.Text, "Text")]
    [InlineData(AttributeDataType.Integer, "Integer")]
    [InlineData(AttributeDataType.Decimal, "Decimal")]
    [InlineData(AttributeDataType.Boolean, "Boolean")]
    [InlineData(AttributeDataType.Date, "Date")]
    [InlineData(AttributeDataType.Choice, "Choice")]
    [InlineData(AttributeDataType.MultiChoice, "MultiChoice")]
    public async Task MapsDefinitionToExistingReadContract(AttributeDataType dataType, string expectedType)
    {
        var store = new ProductTypeManagementScenario.Store();
        var type = ProductType.Create("Type");
        var attribute = type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("value"), " Value ",
            dataType, true, false, AttributeScope.Variant);
        store.Items.Add(type.Id, type);
        var result = await GetProductTypeAttributeEndpoint.ExecuteAsync(type.Id.Value, attribute.Id.Value,
            new UseCase(store), CancellationToken.None);
        var value = Assert.IsType<Ok<AttributeDefinitionResponse>>(result.Result).Value!;
        Assert.Equal(attribute.Id.Value, value.Id);
        Assert.Equal("value", value.Code);
        Assert.Equal(" Value ", value.DisplayName);
        Assert.Equal(expectedType, value.DataType);
        Assert.Equal("Variant", value.Scope);
        Assert.True(value.IsRequired);
        Assert.False(value.IsFilterable);
        Assert.Equal(0, store.Saves);
    }

    [Theory]
    [InlineData(false, "Attribute not found")]
    [InlineData(true, "Product type not found")]
    public async Task MissingResourceReturnsSpecificNotFound(bool missingType, string title)
    {
        var store = new ProductTypeManagementScenario.Store();
        var type = ProductType.Create("Type");
        if (!missingType) store.Items.Add(type.Id, type);
        var result = await GetProductTypeAttributeEndpoint.ExecuteAsync(type.Id.Value, Guid.NewGuid(),
            new UseCase(store), CancellationToken.None);
        Assert.Equal(title, Assert.IsType<NotFound<ProblemDetails>>(result.Result).Value!.Title);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EmptyIdentifierIsBadRequestBeforeRead(bool emptyType)
    {
        var store = new ProductTypeManagementScenario.Store();
        var result = await GetProductTypeAttributeEndpoint.ExecuteAsync(
            emptyType ? Guid.Empty : Guid.NewGuid(), emptyType ? Guid.NewGuid() : Guid.Empty,
            new UseCase(store), CancellationToken.None);
        Assert.Equal("Invalid product type attribute ID",
            Assert.IsType<BadRequest<ProblemDetails>>(result.Result).Value!.Title);
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task PreCancelledRequestDoesNotRead()
    {
        var store = new ProductTypeManagementScenario.Store();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => GetProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), new UseCase(store), source.Token));
        Assert.Equal(0, store.Reads);
    }

    [Fact]
    public async Task RejectsNullUseCase() =>
        await Assert.ThrowsAsync<ArgumentNullException>(() => GetProductTypeAttributeEndpoint.ExecuteAsync(
            Guid.NewGuid(), Guid.NewGuid(), null!, CancellationToken.None));
}
