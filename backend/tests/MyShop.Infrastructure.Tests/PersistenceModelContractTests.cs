using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Tests;

public sealed class PersistenceModelContractTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(ProductPersistence).Assembly;
    private static readonly Type[] Models = InfrastructureAssembly.GetTypes()
        .Where(type => type.Namespace == typeof(ProductPersistence).Namespace
            && type.Name.EndsWith("Persistence", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();
    private static readonly Type[] Configurations = InfrastructureAssembly.GetTypes()
        .Where(type => type.Namespace == "MyShop.Infrastructure.Persistence.Configurations"
            && type.Name.EndsWith("PersistenceConfiguration", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> ModelData => new(Models);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_IsInternal(Type modelType) => Assert.False(modelType.IsPublic);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_IsSealed(Type modelType) => Assert.True(modelType.IsSealed);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_UsesModelsNamespace(Type modelType) =>
        Assert.Equal(typeof(ProductPersistence).Namespace, modelType.Namespace);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_HasParameterlessConstructor(Type modelType) =>
        Assert.Contains(
            modelType.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic),
            constructor => constructor.GetParameters().Length == 0);

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_HasGuidKeyProperty(Type modelType)
    {
        var keys = modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.Name.EndsWith("Id", StringComparison.Ordinal)
                && property.PropertyType == typeof(Guid));

        Assert.NotEmpty(keys);
    }

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_PropertiesAreWritable(Type modelType) =>
        Assert.All(modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance), property =>
            Assert.NotNull(property.SetMethod));

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_HasProperties(Type modelType) =>
        Assert.NotEmpty(modelType.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_HasMatchingConfiguration(Type modelType) =>
        Assert.NotNull(GetConfiguration(modelType));

    [Theory]
    [MemberData(nameof(ModelData))]
    public void PersistenceModel_ConfigurationTargetsModel(Type modelType)
    {
        var configuration = GetConfiguration(modelType);
        Assert.NotNull(configuration);
        var contract = Assert.Single(configuration.GetInterfaces(), type =>
            type.IsGenericType
            && type.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

        Assert.Equal(modelType, contract.GetGenericArguments()[0]);
    }

    [Fact]
    public void PersistenceModelInventory_ContainsExpectedNumberOfModels() => Assert.Equal(10, Models.Length);

    [Fact]
    public void PersistenceModelInventory_ContainsExpectedNumberOfConfigurations() => Assert.Equal(10, Configurations.Length);

    [Fact]
    public void PersistenceModelInventory_HasUniqueModelNames() =>
        Assert.Equal(Models.Length, Models.Select(type => type.Name).Distinct().Count());

    [Fact]
    public void PersistenceModelInventory_HasUniqueConfigurationNames() =>
        Assert.Equal(Configurations.Length, Configurations.Select(type => type.Name).Distinct().Count());

    [Fact]
    public void PersistenceModelInventory_ContainsOnlyInternalModels() =>
        Assert.All(Models, type => Assert.False(type.IsPublic));

    [Fact]
    public void PersistenceModelInventory_ContainsOnlySealedModels() =>
        Assert.All(Models, type => Assert.True(type.IsSealed));

    [Fact]
    public void PersistenceModelInventory_ContainsOnlyConcreteModels() =>
        Assert.All(Models, type => Assert.False(type.IsAbstract));

    [Fact]
    public void PersistenceModelInventory_ContainsGuidKeys() =>
        Assert.All(Models, model => Assert.Contains(model.GetProperties(), property =>
            property.Name.EndsWith("Id", StringComparison.Ordinal)
            && property.PropertyType == typeof(Guid)));

    [Fact]
    public void PersistenceModelInventory_EachModelHasConfiguration() =>
        Assert.All(Models, model => Assert.NotNull(GetConfiguration(model)));

    [Fact]
    public void PersistenceModelInventory_ConfigurationsUseExpectedNamespace() =>
        Assert.All(Configurations, configuration =>
            Assert.Equal("MyShop.Infrastructure.Persistence.Configurations", configuration.Namespace));

    private static Type? GetConfiguration(Type modelType) =>
        Configurations.SingleOrDefault(configuration =>
            configuration.Name == $"{modelType.Name}Configuration");
}
