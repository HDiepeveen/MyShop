using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using MyShop.Application.Notifications;
using MyShop.Infrastructure.Notifications;

namespace MyShop.Infrastructure.Tests.Integration;
[Collection(SqlServerCollection.Name)]
public sealed class EmailSettingsStorageTests(SqlServerDatabase database)
{
    [SqlServerFact]
    public async Task Encrypts_preserves_replaces_and_clears_password_with_revision_control()
    {
        var protection = new EphemeralDataProtectionProvider();
        var configuration = new ConfigurationBuilder().Build(); var environment = new Environment();
        await using var context = database.CreateContext();
        var repository = new EmailSettingsRepository(context, configuration, environment, protection);
        var current = await repository.GetAsync(CancellationToken.None);
        var command = new UpdateEmailSettingsCommand(true, "smtp.example.test", 587, "user", "shop@example.test", "MyShop", "https://shop.example.test", " secret password ", false, current.Revision);
        var saved = (await repository.SaveAsync(command, CancellationToken.None))!;
        var raw = await context.EmailSettings.AsNoTracking().SingleAsync();
        Assert.NotEqual(" secret password ", raw.ProtectedPassword);
        Assert.DoesNotContain("secret password", raw.ProtectedPassword!);
        Assert.Equal(" secret password ", (await repository.RuntimeAsync(CancellationToken.None)).Password);
        Assert.True(saved.PasswordConfigured);
        Assert.Null(await repository.SaveAsync(command with { Password = "stale" }, CancellationToken.None));
        var preserved = (await repository.SaveAsync(command with { Revision = saved.Revision, Password = null }, CancellationToken.None))!;
        Assert.Equal(" secret password ", (await repository.RuntimeAsync(CancellationToken.None)).Password);
        var replaced = (await repository.SaveAsync(command with { Revision = preserved.Revision, Password = "new password" }, CancellationToken.None))!;
        Assert.Equal("new password", (await repository.RuntimeAsync(CancellationToken.None)).Password);
        var cleared = (await repository.SaveAsync(command with { Revision = replaced.Revision, Password = null, ClearPassword = true }, CancellationToken.None))!;
        Assert.False(cleared.PasswordConfigured); Assert.Null((await repository.RuntimeAsync(CancellationToken.None)).Password);
    }
    private sealed class Environment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "MyShop";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
