using System.Reflection;
using System.Runtime.CompilerServices;
using CreateProductEndpointType = MyShop.Api.Catalog.Products.CreateProductEndpoint;

namespace MyShop.Api.Tests.Catalog;

public sealed class CatalogRequestContractTests
{
    private static readonly Assembly ApiAssembly = typeof(CreateProductEndpointType).Assembly;

    private static readonly Type[] RequestTypes = DiscoverRequestTypes()
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> RequestTypeData => new(RequestTypes);

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_IsPublicSealedClass(Type requestType)
    {
        Assert.True(requestType.IsPublic, $"{requestType.FullName} must be public.");
        Assert.True(requestType.IsClass, $"{requestType.FullName} must be a class.");
        Assert.True(requestType.IsSealed, $"{requestType.FullName} must be sealed.");
    }

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_UsesRequestSuffix(Type requestType) =>
        Assert.EndsWith("Request", requestType.Name, StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_DeclaresSinglePublicConstructor(Type requestType) =>
        Assert.Single(requestType.GetConstructors());

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_ConstructorCoversEveryDeclaredProperty(Type requestType)
    {
        var constructor = Assert.Single(requestType.GetConstructors());
        var properties = GetContractProperties(requestType);

        Assert.Equal(properties.Length, constructor.GetParameters().Length);
    }

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_ConstructorParametersMatchProperties(Type requestType)
    {
        var constructor = Assert.Single(requestType.GetConstructors());
        var properties = GetContractProperties(requestType);

        Assert.All(constructor.GetParameters(), parameter =>
        {
            var property = Assert.Single(properties, candidate =>
                string.Equals(candidate.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(property.PropertyType, parameter.ParameterType);
        });
    }

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_PropertiesArePubliclyReadableAndInitOnly(Type requestType)
    {
        Assert.All(GetContractProperties(requestType), property =>
        {
            Assert.True(property.GetMethod?.IsPublic);
            Assert.True(property.SetMethod?.IsPublic);
            Assert.Contains(
                typeof(IsExternalInit),
                property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
        });
    }

    [Theory]
    [MemberData(nameof(RequestTypeData))]
    public void CatalogRequest_DoesNotExposeInternalLayerTypes(Type requestType)
    {
        var exposedTypes = GetContractProperties(requestType)
            .SelectMany(property => FlattenType(property.PropertyType));

        Assert.DoesNotContain(exposedTypes, type =>
            type.Namespace?.StartsWith("MyShop.Domain", StringComparison.Ordinal) == true
            || type.Namespace?.StartsWith("MyShop.Application", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void CatalogRequestInventory_ContainsExpectedNumberOfRequests() =>
        Assert.Equal(17, RequestTypes.Length);

    [Fact]
    public void CatalogRequestInventory_HasUniqueTypeNames() =>
        Assert.Equal(
            RequestTypes.Length,
            RequestTypes.Select(type => type.Name).Distinct(StringComparer.Ordinal).Count());

    private static HashSet<Type> DiscoverRequestTypes()
    {
        var discovered = new HashSet<Type>();
        var endpointHandlers = ApiAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("MyShop.Api.Catalog.", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == "ExecuteAsync");

        foreach (var parameterType in endpointHandlers.SelectMany(method => method.GetParameters())
                     .Select(parameter => parameter.ParameterType))
            DiscoverApiClasses(parameterType, discovered);

        return discovered;
    }

    private static void DiscoverApiClasses(Type type, HashSet<Type> discovered)
    {
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                DiscoverApiClasses(argument, discovered);
        }

        if (type.Assembly != ApiAssembly || !type.IsClass || !discovered.Add(type))
            return;

        foreach (var property in GetContractProperties(type))
            DiscoverApiClasses(property.PropertyType, discovered);
    }

    private static PropertyInfo[] GetContractProperties(Type type) => type.GetProperties(
        BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);

    private static IEnumerable<Type> FlattenType(Type type)
    {
        yield return type;
        if (!type.IsGenericType)
            yield break;

        foreach (var argument in type.GetGenericArguments())
        foreach (var nested in FlattenType(argument))
            yield return nested;
    }
}
