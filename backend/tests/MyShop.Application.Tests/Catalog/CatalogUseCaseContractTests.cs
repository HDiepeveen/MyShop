using System.Reflection;
using CreateProductUseCase = MyShop.Application.Catalog.CreateProduct.CreateProduct;

namespace MyShop.Application.Tests.Catalog;

public sealed class CatalogUseCaseContractTests
{
    private static readonly Type[] UseCases = typeof(CreateProductUseCase).Assembly.GetTypes()
        .Where(IsCatalogUseCase)
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> UseCaseData => new(UseCases);

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_DeclaresExactlyOnePublicExecuteAsync(Type useCaseType)
    {
        var methods = GetExecuteMethods(useCaseType);

        Assert.Single(methods);
    }

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_AcceptsCancellationTokenAsFinalParameter(Type useCaseType)
    {
        var method = Assert.Single(GetExecuteMethods(useCaseType));
        var parameters = method.GetParameters();

        Assert.NotEmpty(parameters);
        Assert.Equal(typeof(CancellationToken), parameters[^1].ParameterType);
    }

    [Theory]
    [MemberData(nameof(UseCaseData))]
    public void CatalogUseCase_ReturnsTask(Type useCaseType)
    {
        var method = Assert.Single(GetExecuteMethods(useCaseType));

        Assert.True(
            method.ReturnType == typeof(Task)
            || method.ReturnType.IsGenericType
                && method.ReturnType.GetGenericTypeDefinition() == typeof(Task<>),
            $"{useCaseType.FullName}.{method.Name} must return Task or Task<T>.");
    }

    [Fact]
    public void CatalogUseCaseInventory_ContainsExpectedNumberOfUseCases() =>
        Assert.Equal(41, UseCases.Length);

    [Fact]
    public void CatalogUseCaseInventory_ContainsOnlyPublicSealedClasses() =>
        Assert.All(UseCases, type =>
        {
            Assert.True(type.IsPublic, $"{type.FullName} must be public.");
            Assert.True(type.IsSealed, $"{type.FullName} must be sealed.");
            Assert.False(type.IsAbstract, $"{type.FullName} must be concrete.");
        });

    [Fact]
    public void CatalogUseCaseInventory_UsesTypeSpecificNamespaces() =>
        Assert.All(UseCases, type =>
            Assert.EndsWith($".{type.Name}", type.Namespace, StringComparison.Ordinal));

    [Fact]
    public void CatalogUseCaseInventory_DeclaresOnePublicConstructorPerUseCase() =>
        Assert.All(UseCases, type => Assert.Single(type.GetConstructors()));

    private static bool IsCatalogUseCase(Type type) =>
        type.IsClass
        && type.Namespace?.StartsWith("MyShop.Application.Catalog.", StringComparison.Ordinal) == true
        && GetExecuteMethods(type).Length > 0;

    private static MethodInfo[] GetExecuteMethods(Type type) => type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(method => method.Name == "ExecuteAsync")
        .ToArray();
}
