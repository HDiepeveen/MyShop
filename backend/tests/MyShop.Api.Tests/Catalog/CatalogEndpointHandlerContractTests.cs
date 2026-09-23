using System.Reflection;
using Microsoft.AspNetCore.Routing;
using CreateProductEndpointType = MyShop.Api.Catalog.Products.CreateProductEndpoint;

namespace MyShop.Api.Tests.Catalog;

public sealed class CatalogEndpointHandlerContractTests
{
    private static readonly Type[] EndpointTypes = typeof(CreateProductEndpointType).Assembly.GetTypes()
        .Where(IsCatalogEndpointType)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> EndpointTypeData => new(EndpointTypes);

    [Theory]
    [MemberData(nameof(EndpointTypeData))]
    public void CatalogEndpoint_DeclaresExactlyOnePublicExecuteAsync(Type endpointType)
    {
        var handlers = GetExecuteMethods(endpointType);

        Assert.Single(handlers);
    }

    [Theory]
    [MemberData(nameof(EndpointTypeData))]
    public void CatalogEndpoint_AcceptsCancellationTokenAsFinalParameter(Type endpointType)
    {
        var handler = Assert.Single(GetExecuteMethods(endpointType));
        var parameters = handler.GetParameters();

        Assert.NotEmpty(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
    }

    [Theory]
    [MemberData(nameof(EndpointTypeData))]
    public void CatalogEndpoint_ReturnsGenericTask(Type endpointType)
    {
        var handler = Assert.Single(GetExecuteMethods(endpointType));

        Assert.True(handler.ReturnType.IsGenericType);
        Assert.Equal(typeof(Task<>), handler.ReturnType.GetGenericTypeDefinition());
    }

    [Fact]
    public void CatalogEndpointHandlerInventory_ContainsExpectedNumberOfEndpoints() =>
        Assert.Equal(36, EndpointTypes.Length);

    [Fact]
    public void CatalogEndpointHandlerInventory_ContainsOnlyPublicStaticClasses() =>
        Assert.All(EndpointTypes, type =>
        {
            Assert.True(type.IsPublic, $"{type.FullName} must be public.");
            Assert.True(type.IsAbstract, $"{type.FullName} must be static.");
            Assert.True(type.IsSealed, $"{type.FullName} must be static.");
        });

    [Fact]
    public void CatalogEndpointHandlerInventory_UsesEndpointTypeNames() =>
        Assert.All(EndpointTypes, type =>
            Assert.EndsWith("Endpoint", type.Name, StringComparison.Ordinal));

    [Fact]
    public void CatalogEndpointHandlerInventory_DeclaresMatchingMapMethod()
    {
        Assert.All(EndpointTypes, type =>
        {
            var expectedName = $"Map{type.Name[..^"Endpoint".Length]}";
            var mapMethod = Assert.Single(GetMapMethods(type));

            Assert.Equal(expectedName, mapMethod.Name);
            Assert.Equal(typeof(IEndpointRouteBuilder), mapMethod.ReturnType);
            Assert.Equal(typeof(IEndpointRouteBuilder), Assert.Single(mapMethod.GetParameters()).ParameterType);
        });
    }

    private static bool IsCatalogEndpointType(Type type) =>
        type.IsClass
        && type.Namespace?.StartsWith("MyShop.Api.Catalog.", StringComparison.Ordinal) == true
        && GetMapMethods(type).Length > 0;

    private static MethodInfo[] GetExecuteMethods(Type type) => type.GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => method.Name == "ExecuteAsync")
        .ToArray();

    private static MethodInfo[] GetMapMethods(Type type) => type.GetMethods(
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
        .Where(method => method.Name.StartsWith("Map", StringComparison.Ordinal)
            && method.ReturnType == typeof(IEndpointRouteBuilder))
        .ToArray();
}
