using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using HDWebFramework.Infrastructure.Security;

namespace HDWebFramework.Infrastructure.Tests;

public sealed class FrameworkSecretProtectionTests
{
    [Theory]
    [InlineData("Email.SmtpPassword.v1")]
    [InlineData("Mollie.TestApiKey.v1")]
    public void Reads_old_application_and_purpose_and_returns_a_new_encrypted_value(string suffix)
    {
        var folder = Path.Combine(Path.GetTempPath(), "HDWebFrameworkKeys_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            var old = DataProtectionProvider.Create(new DirectoryInfo(folder), options => options.SetApplicationName("MyShop"));
            var current = DataProtectionProvider.Create(new DirectoryInfo(folder), options => options.SetApplicationName("HDWebFramework"));
            var encrypted = old.CreateProtector("MyShop." + suffix).Protect("test-only-value");
            var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Hosting:DataProtectionKeyPath"] = folder }).Build();
            var result = FrameworkSecretProtection.Read(current, "HDWebFramework." + suffix, encrypted, configuration, new TestEnvironment());
            Assert.Equal("test-only-value", result.Value);
            Assert.NotNull(result.Replacement);
            Assert.NotEqual(encrypted, result.Replacement);
            Assert.Equal("test-only-value", current.CreateProtector("HDWebFramework." + suffix).Unprotect(result.Replacement));
            Assert.Null(FrameworkSecretProtection.Read(current, "HDWebFramework." + suffix, result.Replacement, configuration, new TestEnvironment()).Replacement);
        }
        finally { Directory.Delete(folder, true); }
    }

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string ApplicationName { get; set; } = "HDWebFramework";
        public string EnvironmentName { get; set; } = "Development";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
