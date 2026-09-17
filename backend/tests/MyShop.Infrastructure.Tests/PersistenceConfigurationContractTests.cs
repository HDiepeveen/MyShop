using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyShop.Infrastructure.Persistence.Configurations;

namespace MyShop.Infrastructure.Tests;

public sealed class PersistenceConfigurationContractTests
{
    private static readonly Assembly InfrastructureAssembly = typeof(ProductPersistenceConfiguration).Assembly;
    private static readonly Type[] Configurations = InfrastructureAssembly.GetTypes()
        .Where(type => type.Namespace == typeof(ProductPersistenceConfiguration).Namespace
            && type.Name.EndsWith("PersistenceConfiguration", StringComparison.Ordinal))
        .OrderBy(type => type.FullName, StringComparer.Ordinal)
        .ToArray();

    public static TheoryData<Type> ConfigurationData => new(Configurations);

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_IsInternal(Type configurationType) => Assert.False(configurationType.IsPublic);

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_IsSealed(Type configurationType) => Assert.True(configurationType.IsSealed);

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_UsesConfigurationsNamespace(Type configurationType) =>
        Assert.Equal(typeof(ProductPersistenceConfiguration).Namespace, configurationType.Namespace);

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_ImplementsEntityTypeConfiguration(Type configurationType)
    {
        var contract = Assert.Single(configurationType.GetInterfaces(), type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>));

        Assert.True(contract.IsGenericType);
    }

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_DeclaresConfigureMethod(Type configurationType) =>
        Assert.Single(GetConfigureMethods(configurationType));

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_ConfigureReturnsVoid(Type configurationType) =>
        Assert.Equal(typeof(void), Assert.Single(GetConfigureMethods(configurationType)).ReturnType);

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_ConfigureAcceptsEntityTypeBuilder(Type configurationType)
    {
        var method = Assert.Single(GetConfigureMethods(configurationType));
        var target = GetTargetType(configurationType);
        var parameter = Assert.Single(method.GetParameters());

        Assert.Equal(typeof(EntityTypeBuilder<>), parameter.ParameterType.GetGenericTypeDefinition());
        Assert.Equal(target, parameter.ParameterType.GetGenericArguments()[0]);
    }

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_ConfigureIsPublicInstance(Type configurationType)
    {
        var method = Assert.Single(GetConfigureMethods(configurationType));

        Assert.True(method.IsPublic);
        Assert.False(method.IsStatic);
    }

    [Theory]
    [MemberData(nameof(ConfigurationData))]
    public void PersistenceConfiguration_HasOnlyParameterlessConstructor(Type configurationType)
    {
        var constructor = Assert.Single(configurationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance));

        Assert.Empty(constructor.GetParameters());
    }

    [Fact]
    public void PersistenceConfigurationInventory_ContainsExpectedNumberOfConfigurations() =>
        Assert.Equal(11, Configurations.Length);

    [Fact]
    public void PersistenceConfigurationInventory_HasUniqueNames() =>
        Assert.Equal(Configurations.Length, Configurations.Select(type => type.Name).Distinct().Count());

    [Fact]
    public void PersistenceConfigurationInventory_ContainsOnlyInternalTypes() =>
        Assert.All(Configurations, type => Assert.False(type.IsPublic));

    [Fact]
    public void PersistenceConfigurationInventory_ContainsOnlySealedTypes() =>
        Assert.All(Configurations, type => Assert.True(type.IsSealed));

    [Fact]
    public void PersistenceConfigurationInventory_UsesExpectedNamespace() =>
        Assert.All(Configurations, type =>
            Assert.Equal("MyShop.Infrastructure.Persistence.Configurations", type.Namespace));

    [Fact]
    public void PersistenceConfigurationInventory_ImplementsEntityTypeConfiguration() =>
        Assert.All(Configurations, type => Assert.Contains(type.GetInterfaces(), candidate =>
            candidate.IsGenericType && candidate.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>)));

    [Fact]
    public void PersistenceConfigurationInventory_UsesConcreteTargets() =>
        Assert.All(Configurations, type => Assert.False(GetTargetType(type).IsAbstract));

    [Fact]
    public void PersistenceConfigurationInventory_UsesMatchingNames() =>
        Assert.All(Configurations, type =>
            Assert.EndsWith("PersistenceConfiguration", type.Name, StringComparison.Ordinal));

    [Fact]
    public void PersistenceConfigurationInventory_DeclaresPublicConfigureMethods() =>
        Assert.All(Configurations, type =>
        {
            var method = Assert.Single(GetConfigureMethods(type));
            Assert.True(method.IsPublic);
        });

    [Fact]
    public void PersistenceConfigurationInventory_DeclaresUniqueTargets() =>
        Assert.Equal(Configurations.Length, Configurations.Select(GetTargetType).Distinct().Count());

    private static MethodInfo[] GetConfigureMethods(Type configurationType) => configurationType.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
        .Where(method => method.Name == "Configure")
        .ToArray();

    private static Type GetTargetType(Type configurationType) =>
        Assert.Single(configurationType.GetInterfaces(), type =>
            type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IEntityTypeConfiguration<>))
        .GetGenericArguments()[0];
}
