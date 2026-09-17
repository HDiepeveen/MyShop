using System.Reflection;
using System.Runtime.CompilerServices;
using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class AttributeValueDiscriminatorContractTests
{
    private static readonly (Type Type, AttributeDataType DataType)[] Contracts =
    [
        (typeof(TextAttributeValue), AttributeDataType.Text),
        (typeof(IntegerAttributeValue), AttributeDataType.Integer),
        (typeof(DecimalAttributeValue), AttributeDataType.Decimal),
        (typeof(BooleanAttributeValue), AttributeDataType.Boolean),
        (typeof(DateAttributeValue), AttributeDataType.Date),
        (typeof(ChoiceAttributeValue), AttributeDataType.Choice),
        (typeof(MultiChoiceAttributeValue), AttributeDataType.MultiChoice)
    ];

    public static TheoryData<Type> AttributeValueTypeData => new(Contracts.Select(contract => contract.Type));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_IsPublicSealedClass(Type valueType)
    {
        Assert.True(valueType.IsPublic);
        Assert.True(valueType.IsClass);
        Assert.True(valueType.IsSealed);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_IsRecord(Type valueType) =>
        Assert.NotNull(valueType.GetMethod("PrintMembers", BindingFlags.NonPublic | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_InheritsAttributeValue(Type valueType) =>
        Assert.True(typeof(AttributeValue).IsAssignableFrom(valueType));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_UsesCatalogNamespace(Type valueType) =>
        Assert.Equal("MyShop.Domain.Catalog", valueType.Namespace);

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_ExposesCreateFactory(Type valueType) =>
        Assert.NotNull(valueType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_CreateIsPublicStatic(Type valueType)
    {
        var method = valueType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(method);
        Assert.True(method!.IsPublic);
        Assert.True(method.IsStatic);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_CreateReturnsConcreteType(Type valueType) =>
        Assert.Equal(valueType, valueType.GetMethod("Create")!.ReturnType);

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_CreateRequiresInputs(Type valueType) =>
        Assert.NotEmpty(valueType.GetMethod("Create")!.GetParameters());

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_HidesPublicConstructors(Type valueType) =>
        Assert.Empty(valueType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_UsesNonPublicConstruction(Type valueType) =>
        Assert.NotEmpty(valueType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_ExposesTypedDiscriminator(Type valueType)
    {
        var property = valueType.GetProperty(nameof(AttributeValue.DataType));

        Assert.NotNull(property);
        Assert.Equal(typeof(AttributeDataType), property!.PropertyType);
        Assert.NotNull(property.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_ExposesReadOnlyDefinitionIdentity(Type valueType)
    {
        var property = valueType.GetProperty(nameof(AttributeValue.AttributeDefinitionId));

        Assert.NotNull(property);
        Assert.Equal(typeof(AttributeDefinitionId), property!.PropertyType);
        Assert.Null(property.SetMethod);
    }

    [Theory]
    [MemberData(nameof(AttributeValueTypeData))]
    public void AttributeValue_ExposesReadOnlyTypedPayload(Type valueType)
    {
        var propertyName = valueType == typeof(MultiChoiceAttributeValue) ? "Values" : "Value";
        var property = valueType.GetProperty(propertyName);

        Assert.NotNull(property);
        Assert.NotNull(property!.GetMethod);
        Assert.Null(property.SetMethod);
    }

    [Fact]
    public void AttributeValueInventory_ContainsExpectedNumberOfTypes() =>
        Assert.Equal(7, Contracts.Length);

    [Fact]
    public void AttributeValueInventory_HasUniqueTypeNames() =>
        Assert.Equal(Contracts.Length, Contracts.Select(contract => contract.Type.Name).Distinct().Count());

    [Fact]
    public void AttributeValueInventory_ContainsOnlySealedTypes() =>
        Assert.All(Contracts, contract => Assert.True(contract.Type.IsSealed));

    [Fact]
    public void AttributeValueInventory_ContainsOnlyAttributeValues() =>
        Assert.All(Contracts, contract => Assert.True(typeof(AttributeValue).IsAssignableFrom(contract.Type)));

    [Fact]
    public void AttributeValueInventory_ContainsOnlyCreateFactories() =>
        Assert.All(Contracts, contract => Assert.NotNull(contract.Type.GetMethod("Create")));

    [Fact]
    public void AttributeValueInventory_CreateFactoriesReturnConcreteTypes() =>
        Assert.All(Contracts, contract =>
            Assert.Equal(contract.Type, contract.Type.GetMethod("Create")!.ReturnType));

    [Fact]
    public void AttributeValueInventory_UsesUniqueDiscriminators() =>
        Assert.Equal(Contracts.Length, Contracts.Select(contract => contract.DataType).Distinct().Count());

    [Fact]
    public void AttributeValueInventory_UsesExpectedDiscriminators() =>
        Assert.All(Contracts, contract =>
            Assert.Equal(contract.DataType, contract.Type.GetProperty(nameof(AttributeValue.DataType))!.GetValue(
                CreateUninitialized(contract.Type))));

    [Fact]
    public void AttributeValueInventory_UsesDomainNamespace() =>
        Assert.All(Contracts, contract =>
            Assert.Equal("MyShop.Domain.Catalog", contract.Type.Namespace));

    private static object CreateUninitialized(Type type) =>
        RuntimeHelpers.GetUninitializedObject(type);
}
