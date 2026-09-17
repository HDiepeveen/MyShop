using System.Reflection;
using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class CatalogValueObjectContractTests
{
    private static readonly Type[] ValueObjectTypes =
    [
        typeof(AttributeCode),
        typeof(ChoiceValue),
        typeof(Sku),
        typeof(TextAttributeValue),
        typeof(IntegerAttributeValue),
        typeof(DecimalAttributeValue),
        typeof(BooleanAttributeValue),
        typeof(DateAttributeValue),
        typeof(ChoiceAttributeValue),
        typeof(MultiChoiceAttributeValue)
    ];

    public static TheoryData<Type> ValueObjectTypeData => new(ValueObjectTypes);

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_IsPublicSealedClass(Type valueObjectType)
    {
        Assert.True(valueObjectType.IsPublic);
        Assert.True(valueObjectType.IsClass);
        Assert.True(valueObjectType.IsSealed);
    }

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_IsRecord(Type valueObjectType) =>
        Assert.NotNull(valueObjectType.GetMethod(
            "PrintMembers", BindingFlags.Instance | BindingFlags.NonPublic));

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_UsesCatalogNamespace(Type valueObjectType) =>
        Assert.Equal("MyShop.Domain.Catalog", valueObjectType.Namespace);

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_ExposesStaticCreateFactory(Type valueObjectType)
    {
        var create = valueObjectType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(create);
        Assert.Equal(valueObjectType, create!.ReturnType);
    }

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_HidesConstructors(Type valueObjectType) =>
        Assert.Empty(valueObjectType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_HasPrivateOrProtectedConstruction(Type valueObjectType) =>
        Assert.NotEmpty(valueObjectType.GetConstructors(BindingFlags.NonPublic | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_UsesReadOnlyPublicProperties(Type valueObjectType)
    {
        var properties = valueObjectType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        Assert.NotEmpty(properties);
        Assert.All(properties, property =>
        {
            Assert.NotNull(property.GetMethod);
            Assert.Null(property.SetMethod);
        });
    }

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_CreateIsPublicStatic(Type valueObjectType)
    {
        var create = valueObjectType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(create);
        Assert.True(create!.IsPublic);
        Assert.True(create.IsStatic);
    }

    [Theory]
    [MemberData(nameof(ValueObjectTypeData))]
    public void CatalogValueObject_CreateHasAtLeastOneInput(Type valueObjectType)
    {
        var create = valueObjectType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);

        Assert.NotNull(create);
        Assert.NotEmpty(create!.GetParameters());
    }

    [Fact]
    public void CatalogValueObjectInventory_ContainsExpectedNumberOfTypes() =>
        Assert.Equal(10, ValueObjectTypes.Length);

    [Fact]
    public void CatalogValueObjectInventory_HasUniqueTypeNames() =>
        Assert.Equal(ValueObjectTypes.Length, ValueObjectTypes.Select(type => type.Name).Distinct().Count());

    [Fact]
    public void CatalogValueObjectInventory_ContainsOnlySealedTypes() =>
        Assert.All(ValueObjectTypes, type => Assert.True(type.IsSealed));

    [Fact]
    public void CatalogValueObjectInventory_ContainsNoPublicConstructors() =>
        Assert.All(ValueObjectTypes, type =>
            Assert.Empty(type.GetConstructors(BindingFlags.Public | BindingFlags.Instance)));

    [Fact]
    public void CatalogValueObjectInventory_ContainsOnlyStaticCreateFactories() =>
        Assert.All(ValueObjectTypes, type =>
        {
            var create = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
            Assert.NotNull(create);
            Assert.True(create!.IsStatic);
        });

    [Fact]
    public void CatalogValueObjectInventory_ContainsOnlyReadOnlyProperties() =>
        Assert.All(ValueObjectTypes, type =>
            Assert.All(type.GetProperties(BindingFlags.Public | BindingFlags.Instance),
                property => Assert.Null(property.SetMethod)));

    [Fact]
    public void CatalogValueObjectInventory_UsesCatalogNamespace() =>
        Assert.All(ValueObjectTypes, type =>
            Assert.Equal("MyShop.Domain.Catalog", type.Namespace));

    [Fact]
    public void CatalogValueObjectInventory_FactoriesReturnDeclaringType() =>
        Assert.All(ValueObjectTypes, type =>
            Assert.Equal(type, type.GetMethod("Create")!.ReturnType));

    [Fact]
    public void CatalogValueObjectInventory_AttributeValuesRemainDomainValues() =>
        Assert.All(ValueObjectTypes.Where(type => type.Name.EndsWith("AttributeValue", StringComparison.Ordinal)),
            type => Assert.True(typeof(AttributeValue).IsAssignableFrom(type)));

    [Fact]
    public void CatalogValueObjectInventory_HasNoValueTypeImplementations() =>
        Assert.All(ValueObjectTypes, type => Assert.False(type.IsValueType));
}
