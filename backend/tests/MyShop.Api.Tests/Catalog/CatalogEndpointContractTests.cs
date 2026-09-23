using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Api.Catalog.Products;
using MyShop.Api.Catalog.ProductTypes;

namespace MyShop.Api.Tests.Catalog;

public sealed class CatalogEndpointContractTests
{
    private static readonly RouteContract[] Contracts =
    [
        new("AddProductTypeAttribute", "POST", "/api/product-types/{productTypeId:guid}/attributes"),
        new("AddProductVariant", "POST", "/api/products/{productId:guid}/variants"),
        new("AssignProductToCategory", "PUT", "/api/products/{productId:guid}/categories/{categoryId:guid}"),
        new("ClearProductVariantSku", "DELETE", "/api/products/{productId:guid}/variants/{variantId:guid}/sku"),
        new("ConfigureProductTypeAttribute", "PUT", "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/configuration"),
        new("CreateCategory", "POST", "/api/categories"),
        new("CreateProduct", "POST", "/api/products"),
        new("CreateProductType", "POST", "/api/product-types"),
        new("DeleteCategory", "DELETE", "/api/categories/{categoryId:guid}"),
        new("DeleteProduct", "DELETE", "/api/products/{productId:guid}"),
        new("DeleteProductType", "DELETE", "/api/product-types/{productTypeId:guid}"),
        new("GetCategory", "GET", "/api/categories/{categoryId:guid}"),
        new("GetProduct", "GET", "/api/products/{productId:guid}"),
        new("GetProductBySku", "GET", "/api/products/by-sku/{sku}"),
        new("GetProductType", "GET", "/api/product-types/{productTypeId:guid}"),
        new("ListCategories", "GET", "/api/categories"),
        new("ListProducts", "GET", "/api/products"),
        new("ListProductTypes", "GET", "/api/product-types"),
        new("MoveCategory", "PUT", "/api/categories/{categoryId:guid}/parent"),
        new("RemoveProductAttributeValue", "DELETE", "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}"),
        new("RemoveProductFromCategory", "DELETE", "/api/products/{productId:guid}/categories/{categoryId:guid}"),
        new("RemoveProductTypeAttribute", "DELETE", "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}"),
        new("RemoveProductVariant", "DELETE", "/api/products/{productId:guid}/variants/{variantId:guid}"),
        new("RemoveVariantAttributeValue", "DELETE", "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}"),
        new("RenameCategory", "PATCH", "/api/categories/{categoryId:guid}/name"),
        new("RenameProduct", "PATCH", "/api/products/{productId:guid}/name"),
        new("RenameProductType", "PATCH", "/api/product-types/{productTypeId:guid}/name"),
        new("RenameProductTypeAttribute", "PATCH", "/api/product-types/{productTypeId:guid}/attributes/{attributeId:guid}/name"),
        new("RenameProductVariant", "PATCH", "/api/products/{productId:guid}/variants/{variantId:guid}/name"),
        new("SetProductAttributeValue", "PUT", "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}"),
        new("ClearProductVariantPrice", "DELETE", "/api/products/{productId:guid}/variants/{variantId:guid}/price"),
        new("AddProductVariantPriceRule", "POST", "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules"),
        new("RemoveProductVariantPriceRule", "DELETE", "/api/products/{productId:guid}/variants/{variantId:guid}/price-rules/{priceRuleId:guid}"),
        new("SetProductVariantPrice", "PUT", "/api/products/{productId:guid}/variants/{variantId:guid}/price"),
        new("SetProductVariantSku", "PUT", "/api/products/{productId:guid}/variants/{variantId:guid}/sku"),
        new("SetVariantAttributeValue", "PUT", "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}")
    ];

    private static readonly IReadOnlyList<RouteEndpoint> Endpoints = CreateEndpoints();

    public static TheoryData<RouteContract> ContractData => new(Contracts);

    [Theory]
    [MemberData(nameof(ContractData))]
    public void CatalogEndpoint_HasExpectedRoute(RouteContract contract)
    {
        var endpoint = FindEndpoint(contract.Name);

        Assert.Equal(contract.Route, endpoint.RoutePattern.RawText);
    }

    [Theory]
    [MemberData(nameof(ContractData))]
    public void CatalogEndpoint_HasExpectedHttpMethod(RouteContract contract)
    {
        var endpoint = FindEndpoint(contract.Name);

        Assert.Equal([contract.Method], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Theory]
    [MemberData(nameof(ContractData))]
    public void CatalogEndpoint_HasExpectedName(RouteContract contract)
    {
        var endpoint = FindEndpoint(contract.Name);

        Assert.Equal(contract.Name, endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
    }

    [Fact]
    public void CatalogEndpointInventory_ContainsExpectedNumberOfEndpoints() =>
        Assert.Equal(Contracts.Length, GetEndpoints().Count);

    [Fact]
    public void CatalogEndpointInventory_HasUniqueNames()
    {
        var names = GetEndpoints()
            .Select(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName)
            .ToArray();

        Assert.Equal(names.Length, names.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CatalogEndpointInventory_HasUniqueMethodAndRoutePairs()
    {
        var operations = GetEndpoints().Select(endpoint =>
        {
            var method = Assert.Single(endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
            return $"{method} {endpoint.RoutePattern.RawText}";
        }).ToArray();

        Assert.Equal(operations.Length, operations.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void CatalogEndpointInventory_CoversEveryCatalogMapExtension()
    {
        var mapMethodNames = typeof(CreateProductEndpoint).Assembly.GetTypes()
            .Where(type => type.IsAbstract && type.IsSealed
                && type.Namespace?.StartsWith("MyShop.Api.Catalog.", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetMethods())
            .Where(method => method.IsPublic && method.IsStatic
                && method.Name.StartsWith("Map", StringComparison.Ordinal)
                && method.ReturnType == typeof(IEndpointRouteBuilder))
            .Select(method => method.Name[3..])
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            Contracts.Select(contract => contract.Name).OrderBy(name => name, StringComparer.Ordinal),
            mapMethodNames);
    }

    private static RouteEndpoint FindEndpoint(string name) =>
        Assert.Single(GetEndpoints(), endpoint =>
            endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == name);

    private static IReadOnlyList<RouteEndpoint> GetEndpoints() => Endpoints;

    private static IReadOnlyList<RouteEndpoint> CreateEndpoints()
    {
        using var app = WebApplication.CreateBuilder().Build();
        MapCatalog(app);

        return ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints)
            .Cast<RouteEndpoint>()
            .ToArray();
    }

    private static void MapCatalog(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapCreateProduct();
        endpoints.MapDeleteProduct();
        endpoints.MapClearProductVariantSku();
        endpoints.MapCreateCategory();
        endpoints.MapCreateProductType();
        endpoints.MapDeleteCategory();
        endpoints.MapDeleteProductType();
        endpoints.MapConfigureProductTypeAttribute();
        endpoints.MapGetCategory();
        endpoints.MapListCategories();
        endpoints.MapGetProduct();
        endpoints.MapGetProductBySku();
        endpoints.MapListProducts();
        endpoints.MapListProductTypes();
        endpoints.MapMoveCategory();
        endpoints.MapRenameProductType();
        endpoints.MapRenameProductTypeAttribute();
        endpoints.MapGetProductType();
        endpoints.MapRenameProduct();
        endpoints.MapRenameCategory();
        endpoints.MapAddProductVariant();
        endpoints.MapAddProductTypeAttribute();
        endpoints.MapRenameProductVariant();
        endpoints.MapSetProductVariantSku();
        endpoints.MapSetProductVariantPrice();
        endpoints.MapRemoveProductVariantPriceRule();
        endpoints.MapAddProductVariantPriceRule();
        endpoints.MapClearProductVariantPrice();
        endpoints.MapRemoveProductVariant();
        endpoints.MapAssignProductToCategory();
        endpoints.MapRemoveProductFromCategory();
        endpoints.MapRemoveProductAttributeValue();
        endpoints.MapRemoveProductTypeAttribute();
        endpoints.MapRemoveVariantAttributeValue();
        endpoints.MapSetVariantAttributeValue();
        endpoints.MapSetProductAttributeValue();
    }

    public sealed record RouteContract(string Name, string Method, string Route);
}
