using System.Reflection;
using Microsoft.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests;

public sealed class PersistenceContextContractTests
{
    private static readonly Type ContextType = typeof(MyShopDbContext);
    private static readonly Type[] Models = typeof(ProductPersistence).Assembly.GetTypes()
        .Where(type => type.Namespace == typeof(ProductPersistence).Namespace
            && type.Name.EndsWith("Persistence", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> ModelData => new(Models);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_ExposesDbSetForModel(Type modelType) =>
        Assert.NotNull(GetDbSet(modelType));

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetIsInternal(Type modelType) =>
        Assert.False(GetDbSet(modelType)!.GetMethod!.IsPublic);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetIsInstanceProperty(Type modelType) =>
        Assert.False(GetDbSet(modelType)!.GetMethod!.IsStatic);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetHasNoSetter(Type modelType) =>
        Assert.Null(GetDbSet(modelType)!.SetMethod);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetUsesGenericDbSetType(Type modelType) =>
        Assert.Equal(typeof(DbSet<>), GetDbSet(modelType)!.PropertyType.GetGenericTypeDefinition());

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetTargetsModel(Type modelType) =>
        Assert.Equal(modelType, GetDbSet(modelType)!.PropertyType.GetGenericArguments()[0]);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetGetterIsInternal(Type modelType) =>
        Assert.False(GetDbSet(modelType)!.GetMethod!.IsPublic);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetIsDeclaredOnContext(Type modelType) =>
        Assert.Equal(ContextType, GetDbSet(modelType)!.DeclaringType);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceContext_DbSetNameIsPlural(Type modelType) =>
        Assert.EndsWith("s", GetDbSet(modelType)!.Name, StringComparison.Ordinal);

    [Fact]
    public void PersistenceContext_IsSealed() => Assert.True(ContextType.IsSealed);

    [Fact]
    public void PersistenceContext_DerivesFromDbContext() => Assert.True(ContextType.IsSubclassOf(typeof(DbContext)));

    [Fact]
    public void PersistenceContext_HasSingleConstructor() => Assert.Single(ContextType.GetConstructors());

    [Fact]
    public void PersistenceContext_ConstructorAcceptsMatchingOptions()
    {
        var constructor = Assert.Single(ContextType.GetConstructors());
        var parameter = Assert.Single(constructor.GetParameters());

        Assert.Equal(typeof(DbContextOptions<MyShopDbContext>), parameter.ParameterType);
    }

    [Fact]
    public void PersistenceContext_HasExpectedModelCount() => Assert.Equal(10, Models.Length);

    [Fact]
    public void PersistenceContext_HasExpectedDbSetCount() => Assert.Equal(10, GetDbSets().Length);

    [Fact]
    public void PersistenceContext_HasUniqueDbSetNames() =>
        Assert.Equal(10, GetDbSets().Select(property => property.Name).Distinct().Count());

    [Fact]
    public void PersistenceContext_HasUniqueDbSetTargets() =>
        Assert.Equal(10, GetDbSets().Select(property => property.PropertyType.GetGenericArguments()[0]).Distinct().Count());

    [Fact]
    public void PersistenceContext_AllDbSetsAreInternal() =>
        Assert.All(GetDbSets(), property => Assert.False(property.GetMethod!.IsPublic));

    [Fact]
    public void PersistenceContext_AllDbSetsAreReadOnly() =>
        Assert.All(GetDbSets(), property => Assert.Null(property.SetMethod));

    [Fact]
    public void PersistenceContext_DeclaresOnModelCreatingOverride()
    {
        var method = ContextType.GetMethod("OnModelCreating", BindingFlags.Instance | BindingFlags.NonPublic);

        Assert.NotNull(method);
        Assert.True(method!.IsFamily);
        Assert.True(method.IsVirtual);
        Assert.True(method.GetBaseDefinition() != method);
    }

    private static PropertyInfo? GetDbSet(Type modelType) => GetDbSets()
        .SingleOrDefault(property => property.PropertyType.GetGenericArguments()[0] == modelType);

    private static PropertyInfo[] GetDbSets() => ContextType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic)
        .Where(property => property.PropertyType.IsGenericType
            && property.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>))
        .ToArray();
}
