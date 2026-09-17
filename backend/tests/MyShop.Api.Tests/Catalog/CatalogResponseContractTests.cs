using System.Reflection;
using System.Runtime.CompilerServices;
using CreateProductEndpointType = MyShop.Api.Catalog.Products.CreateProductEndpoint;

namespace MyShop.Api.Tests.Catalog;

public sealed class CatalogResponseContractTests
{
    private static readonly Assembly ApiAssembly = typeof(CreateProductEndpointType).Assembly;

    private static readonly Type[] ResponseTypes = DiscoverResponseTypes()
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> ResponseTypeData => new(ResponseTypes);

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_IsPublicSealedClass(Type responseType)
    {
        Assert.True(responseType.IsPublic, $"{responseType.FullName} must be public.");
        Assert.True(responseType.IsClass, $"{responseType.FullName} must be a class.");
        Assert.True(responseType.IsSealed, $"{responseType.FullName} must be sealed.");
    }

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_UsesResponseSuffix(Type responseType) =>
        Assert.EndsWith("Response", responseType.Name, StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_DeclaresSinglePublicConstructor(Type responseType) =>
        Assert.Single(responseType.GetConstructors());

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_ConstructorCoversEveryDeclaredProperty(Type responseType)
    {
        var constructor = Assert.Single(responseType.GetConstructors());
        var properties = GetContractProperties(responseType);

        Assert.Equal(properties.Length, constructor.GetParameters().Length);
    }

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_ConstructorParametersMatchProperties(Type responseType)
    {
        var constructor = Assert.Single(responseType.GetConstructors());
        var properties = GetContractProperties(responseType);

        Assert.All(constructor.GetParameters(), parameter =>
        {
            var property = Assert.Single(properties, candidate =>
                string.Equals(candidate.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));
            Assert.Equal(property.PropertyType, parameter.ParameterType);
        });
    }

    [Theory]
    [MemberData(nameof(ResponseTypeData))]
    public void CatalogResponse_PropertiesArePubliclyReadableAndInitOnly(Type responseType)
    {
        Assert.All(GetContractProperties(responseType), property =>
        {
            Assert.True(property.GetMethod?.IsPublic);
            Assert.True(property.SetMethod?.IsPublic);
            Assert.Contains(
                typeof(IsExternalInit),
                property.SetMethod!.ReturnParameter.GetRequiredCustomModifiers());
        });
    }

    [Fact]
    public void CatalogResponseInventory_ContainsExpectedNumberOfResponses() =>
        Assert.Equal(16, ResponseTypes.Length);

    [Fact]
    public void CatalogResponseInventory_HasUniqueTypeNames() =>
        Assert.Equal(
            ResponseTypes.Length,
            ResponseTypes.Select(type => type.Name).Distinct(StringComparer.Ordinal).Count());

    [Fact]
    public void CatalogResponseInventory_UsesOnlyCatalogNamespaces() =>
        Assert.All(ResponseTypes, type =>
            Assert.StartsWith("MyShop.Api.Catalog.", type.Namespace, StringComparison.Ordinal));

    [Fact]
    public void CatalogResponseInventory_DoesNotExposeDomainTypes()
    {
        var exposedTypes = ResponseTypes
            .SelectMany(GetContractProperties)
            .SelectMany(property => FlattenType(property.PropertyType));

        Assert.DoesNotContain(exposedTypes, type =>
            type.Namespace?.StartsWith("MyShop.Domain", StringComparison.Ordinal) == true);
    }

    private static HashSet<Type> DiscoverResponseTypes()
    {
        var discovered = new HashSet<Type>();
        var endpointHandlers = ApiAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("MyShop.Api.Catalog.", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly))
            .Where(method => method.Name == "ExecuteAsync");

        foreach (var handler in endpointHandlers)
            DiscoverApiTypes(handler.ReturnType, discovered);

        return discovered;
    }

    private static void DiscoverApiTypes(Type type, HashSet<Type> discovered)
    {
        if (type.IsGenericType)
        {
            foreach (var argument in type.GetGenericArguments())
                DiscoverApiTypes(argument, discovered);
        }

        if (type.Assembly != ApiAssembly || !type.IsClass || !discovered.Add(type))
            return;

        foreach (var property in GetContractProperties(type))
            DiscoverApiTypes(property.PropertyType, discovered);
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
