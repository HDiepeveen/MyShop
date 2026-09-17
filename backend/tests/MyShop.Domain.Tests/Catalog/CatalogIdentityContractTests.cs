using System.Reflection;
using System.Runtime.CompilerServices;
using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class CatalogIdentityContractTests
{
    private static readonly Type[] IdentityTypes =
    [
        typeof(ProductId),
        typeof(ProductVariantId),
        typeof(ProductTypeId),
        typeof(CategoryId),
        typeof(AttributeDefinitionId)
    ];

    public static TheoryData<Type> IdentityTypeData => new(IdentityTypes);

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_IsPublicReadonlyRecordStruct(Type identityType)
    {
        Assert.True(identityType.IsPublic);
        Assert.True(identityType.IsValueType);
        Assert.True(identityType.IsDefined(typeof(IsReadOnlyAttribute), inherit: false));
        Assert.True(identityType.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance) is not null);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_ExposesGuidValue(Type identityType)
    {
        var property = Assert.Single(identityType.GetProperties(BindingFlags.Public | BindingFlags.Instance));

        Assert.Equal("Value", property.Name);
        Assert.Equal(typeof(Guid), property.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_HasPrivateValueConstructor(Type identityType)
    {
        var constructor = Assert.Single(identityType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));

        Assert.Equal([typeof(Guid)], constructor.GetParameters().Select(parameter => parameter.ParameterType));
        Assert.False(constructor.IsPublic);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_HasNoPublicConstructor(Type identityType) =>
        Assert.Empty(identityType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_ExposesNewFactory(Type identityType)
    {
        var method = identityType.GetMethod("New", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.Empty(method!.GetParameters());
        Assert.Equal(identityType, method.ReturnType);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_NewReturnsNonEmptyValue(Type identityType)
    {
        var value = InvokeNew(identityType);

        Assert.NotEqual(Guid.Empty, GetValue(value));
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_NewReturnsDistinctValues(Type identityType)
    {
        var first = GetValue(InvokeNew(identityType));
        var second = GetValue(InvokeNew(identityType));

        Assert.NotEqual(first, second);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_ExposesFromFactory(Type identityType)
    {
        var method = identityType.GetMethod("From", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.Equal(identityType, method!.ReturnType);
        Assert.Equal([typeof(Guid)], method.GetParameters().Select(parameter => parameter.ParameterType));
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_FromReturnsExpectedType(Type identityType)
    {
        var value = Guid.NewGuid();

        Assert.Equal(identityType, InvokeFrom(identityType, value).GetType());
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_FromPreservesGuid(Type identityType)
    {
        var expected = Guid.NewGuid();
        var actual = InvokeFrom(identityType, expected);

        Assert.Equal(expected, GetValue(actual));
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_FromRejectsEmptyGuid(Type identityType)
    {
        var exception = Assert.Throws<TargetInvocationException>(() => InvokeFrom(identityType, Guid.Empty));

        Assert.IsType<ArgumentException>(exception.InnerException);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_FromDoesNotCreateEmptyIdentity(Type identityType)
    {
        var exception = Record.Exception(() => InvokeFrom(identityType, Guid.Empty));

        Assert.NotNull(exception);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_SameValuesAreEqual(Type identityType)
    {
        var value = Guid.NewGuid();

        Assert.Equal(InvokeFrom(identityType, value), InvokeFrom(identityType, value));
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_DifferentValuesAreNotEqual(Type identityType)
    {
        var first = InvokeFrom(identityType, Guid.NewGuid());
        var second = InvokeFrom(identityType, Guid.NewGuid());

        Assert.NotEqual(first, second);
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_EqualValuesHaveEqualHashCodes(Type identityType)
    {
        var value = Guid.NewGuid();

        Assert.Equal(
            InvokeFrom(identityType, value)!.GetHashCode(),
            InvokeFrom(identityType, value)!.GetHashCode());
    }

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_UsesCatalogNamespace(Type identityType) =>
        Assert.Equal("MyShop.Domain.Catalog", identityType.Namespace);

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_UsesIdSuffix(Type identityType) =>
        Assert.EndsWith("Id", identityType.Name, StringComparison.Ordinal);

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_ValueIsOnlyPublicInstanceProperty(Type identityType) =>
        Assert.Single(identityType.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(IdentityTypeData))]
    public void CatalogIdentity_FactoriesArePublicStaticMethods(Type identityType)
    {
        Assert.True(identityType.GetMethod("New", BindingFlags.Public | BindingFlags.Static)!.IsStatic);
        Assert.True(identityType.GetMethod("From", BindingFlags.Public | BindingFlags.Static)!.IsStatic);
    }

    [Fact]
    public void CatalogIdentityInventory_ContainsExpectedNumberOfTypes() =>
        Assert.Equal(5, IdentityTypes.Length);

    [Fact]
    public void CatalogIdentityInventory_HasUniqueTypeNames() =>
        Assert.Equal(IdentityTypes.Length, IdentityTypes.Select(type => type.Name).Distinct().Count());

    [Fact]
    public void CatalogIdentityInventory_UsesGuidValueProperties() =>
        Assert.All(IdentityTypes, type =>
            Assert.Equal(typeof(Guid), type.GetProperty("Value")!.PropertyType));

    [Fact]
    public void CatalogIdentityInventory_UsesMatchingFactoryReturnTypes() =>
        Assert.All(IdentityTypes, type =>
        {
            Assert.Equal(type, type.GetMethod("New")!.ReturnType);
            Assert.Equal(type, type.GetMethod("From")!.ReturnType);
        });

    [Fact]
    public void CatalogIdentityInventory_UsesNoReferenceTypes() =>
        Assert.All(IdentityTypes, type => Assert.True(type.IsValueType));

    private static object InvokeNew(Type identityType) =>
        identityType.GetMethod("New")!.Invoke(null, null)!;

    private static object InvokeFrom(Type identityType, Guid value) =>
        identityType.GetMethod("From")!.Invoke(null, [value])!;

    private static Guid GetValue(object value) =>
        (Guid)value.GetType().GetProperty("Value")!.GetValue(value)!;
}
