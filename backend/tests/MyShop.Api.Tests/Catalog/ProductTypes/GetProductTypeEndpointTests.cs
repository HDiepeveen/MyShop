using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductType.GetProductType;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

public sealed class GetProductTypeEndpointTests
{
    [Fact]
    public void MapGetProductType_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapGetProductType());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/product-types/{productTypeId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("GetProductType", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeExists_ReturnsCompleteResponse()
    {
        var scenario = new Scenario();
        var productAttribute = scenario.ProductType.AddAttribute(
            AttributeDefinitionId.New(),
            AttributeCode.Create("material"),
            "Material",
            AttributeDataType.Text,
            true,
            true,
            AttributeScope.Product);
        var variantAttribute = scenario.ProductType.AddAttribute(
            AttributeDefinitionId.New(),
            AttributeCode.Create("size"),
            "Size",
            AttributeDataType.Choice,
            false,
            false,
            AttributeScope.Variant);

        var result = await scenario.ExecuteAsync();

        var ok = Assert.IsType<Ok<GetProductTypeResponse>>(result.Result);
        var response = Assert.IsType<GetProductTypeResponse>(ok.Value);
        Assert.Equal(scenario.ProductType.Id.Value, response.Id);
        Assert.Equal("Clothing", response.Name);
        Assert.Collection(response.AttributeDefinitions,
            attribute => AssertAttribute(attribute, productAttribute),
            attribute => AssertAttribute(attribute, variantAttribute));
    }

    [Fact]
    public async Task ExecuteAsync_WhenProductTypeDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario { ProductTypeIsMissing = true };

        var result = await scenario.ExecuteAsync();

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Product type not found", notFound.Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequestWithoutRepositoryAccess()
    {
        var scenario = new Scenario();

        var result = await GetProductTypeEndpoint.ExecuteAsync(
            Guid.Empty,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid product type ID", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario();
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.ExecuteAsync(source.Token));

        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    private static void AssertAttribute(
        AttributeDefinitionResponse response,
        AttributeDefinition definition)
    {
        Assert.Equal(definition.Id.Value, response.Id);
        Assert.Equal(definition.Code.Value, response.Code);
        Assert.Equal(definition.DisplayName, response.DisplayName);
        Assert.Equal(definition.DataType.ToString(), response.DataType);
        Assert.Equal(definition.Scope.ToString(), response.Scope);
        Assert.Equal(definition.IsRequired, response.IsRequired);
        Assert.Equal(definition.IsFilterable, response.IsFilterable);
    }

    private sealed class Scenario
    {
        public Scenario()
        {
            ProductType = MyShop.Domain.Catalog.ProductType.Create("Clothing");
            Repository = new ProductTypeRepositoryFake(this);
            UseCase = new UseCase(Repository);
        }

        public ProductType ProductType { get; }
        public bool ProductTypeIsMissing { get; set; }
        public ProductTypeRepositoryFake Repository { get; }
        public UseCase UseCase { get; }

        public Task<Results<Ok<GetProductTypeResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
            ExecuteAsync(CancellationToken cancellationToken = default) =>
            GetProductTypeEndpoint.ExecuteAsync(
                ProductType.Id.Value,
                UseCase,
                cancellationToken);
    }

    private sealed class ProductTypeRepositoryFake(Scenario scenario) : IProductTypeRepository
    {
        public int GetCalls { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.ProductTypeIsMissing ? null : scenario.ProductType);
        }
    }
}
