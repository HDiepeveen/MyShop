using System.Reflection;
using CreateProductFailureType = MyShop.Application.Catalog.CreateProduct.CreateProductFailure;

namespace MyShop.Application.Tests.Catalog;

public sealed class CatalogFailureContractTests
{
    private static readonly Type[] FailureTypes = typeof(CreateProductFailureType).Assembly.GetTypes()
        .Where(type => type.Namespace?.StartsWith("MyShop.Application.Catalog.", StringComparison.Ordinal) == true
            && type.Name.EndsWith("Failure", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> FailureTypeData => new(FailureTypes);

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_IsPublicEnum(Type failureType)
    {
        Assert.True(failureType.IsPublic, $"{failureType.FullName} must be public.");
        Assert.True(failureType.IsEnum, $"{failureType.FullName} must be an enum.");
    }

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_UsesFailureSuffix(Type failureType) =>
        Assert.EndsWith("Failure", failureType.Name, StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_NamespaceMatchesUseCase(Type failureType)
    {
        var useCaseName = failureType.Name[..^"Failure".Length];

        Assert.EndsWith($".{useCaseName}", failureType.Namespace, StringComparison.Ordinal);
    }

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_DeclaresAtLeastOneMember(Type failureType) =>
        Assert.NotEmpty(Enum.GetNames(failureType));

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_UsesUniqueContiguousZeroBasedValues(Type failureType)
    {
        var values = Enum.GetValues(failureType).Cast<int>().Order().ToArray();

        Assert.Equal(Enumerable.Range(0, values.Length), values);
    }

    [Theory]
    [MemberData(nameof(FailureTypeData))]
    public void CatalogFailure_UsesPascalCaseMemberNames(Type failureType)
    {
        Assert.All(Enum.GetNames(failureType), name =>
            Assert.True(char.IsUpper(name[0]), $"{failureType.FullName}.{name} must use PascalCase."));
    }

    [Fact]
    public void CatalogFailureInventory_ContainsExpectedNumberOfFailureTypes() =>
        Assert.Equal(27, FailureTypes.Length);

    [Fact]
    public void CatalogFailureInventory_DoesNotUseFlagsEnums() =>
        Assert.All(FailureTypes, type =>
            Assert.Null(type.GetCustomAttribute<FlagsAttribute>()));

    [Fact]
    public void CatalogFailureInventory_UsesInt32UnderlyingType() =>
        Assert.All(FailureTypes, type =>
            Assert.Equal(typeof(int), Enum.GetUnderlyingType(type)));

    [Fact]
    public void CatalogFailureInventory_EachFailureIsExposedByMatchingResult()
    {
        Assert.All(FailureTypes, failureType =>
        {
            var resultName = $"{failureType.Name[..^"Failure".Length]}Result";
            var resultType = failureType.Assembly.GetType($"{failureType.Namespace}.{resultName}");

            Assert.NotNull(resultType);
            var property = Assert.Single(resultType!.GetProperties(), candidate =>
                candidate.Name == "Failure");
            Assert.Equal(failureType, Nullable.GetUnderlyingType(property.PropertyType));
        });
    }
}
