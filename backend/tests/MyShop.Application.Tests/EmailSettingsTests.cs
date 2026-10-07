using MyShop.Application.Notifications;
namespace MyShop.Application.Tests;
public sealed class EmailSettingsTests
{
    private static UpdateEmailSettingsCommand Command => new(true, "smtp.example.test", 587, "user", "shop@example.test", "MyShop", "https://shop.example.test", null, false, Guid.NewGuid());
    [Theory]
    [InlineData(0)][InlineData(465)][InlineData(65536)]
    public async Task Rejects_unsupported_ports(int port)
    {
        var repository = new Repository();
        await Assert.ThrowsAsync<ArgumentException>(() => new UpdateEmailSettings(repository).ExecuteAsync(Command with { Port = port }, false, CancellationToken.None));
        Assert.Null(repository.Saved);
    }
    [Theory]
    [InlineData("http://external.example")][InlineData("https://shop.example/?token=x")][InlineData("https://shop.example/#x")][InlineData("https://user:secret@shop.example")][InlineData("")]
    public async Task Rejects_untrusted_link_bases(string url) =>
        await Assert.ThrowsAsync<ArgumentException>(() => new UpdateEmailSettings(new Repository()).ExecuteAsync(Command with { PublicBaseUrl = url }, false, CancellationToken.None));
    [Fact]
    public async Task Normalizes_headers_but_preserves_password_characters()
    {
        var repository = new Repository();
        await new UpdateEmailSettings(repository).ExecuteAsync(Command with { Host = " smtp.example.test ", FromName = " MyShop ", Password = " password with spaces " }, false, CancellationToken.None);
        Assert.Equal("smtp.example.test", repository.Saved!.Host);
        Assert.Equal("MyShop", repository.Saved.FromName);
        Assert.Equal(" password with spaces ", repository.Saved.Password);
    }
    [Fact]
    public async Task Rejects_header_injection_and_conflicting_password_choices()
    {
        var useCase = new UpdateEmailSettings(new Repository());
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Command with { FromName = "Shop\r\nBcc: victim" }, false, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Command with { Host = "https://smtp.example.test/path" }, false, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Command with { Password = "secret", ClearPassword = true }, false, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Command with { Enabled = true, Host = "" }, false, CancellationToken.None));
    }
    [Fact]
    public async Task Allows_loopback_http_only_for_development_and_honors_cancellation()
    {
        var repository = new Repository(); var useCase = new UpdateEmailSettings(repository);
        await useCase.ExecuteAsync(Command with { PublicBaseUrl = "http://127.0.0.1:4200" }, true, CancellationToken.None);
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(Command with { PublicBaseUrl = "http://127.0.0.1:4200" }, false, CancellationToken.None));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => useCase.ExecuteAsync(Command, false, new CancellationToken(true)));
    }
    [Fact]
    public async Task Test_mail_requires_enabled_current_settings_and_a_single_recipient()
    {
        var repository = new Repository(); var queue = new Queue(); var useCase = new SendEmailTest(repository, queue);
        Assert.False(await useCase.ExecuteAsync("test@example.test", Guid.NewGuid(), CancellationToken.None)); Assert.Null(queue.Recipient);
        repository.Current = repository.Current with { Enabled = false };
        Assert.False(await useCase.ExecuteAsync("test@example.test", repository.Current.Revision, CancellationToken.None));
        repository.Current = repository.Current with { Enabled = true };
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync("User <test@example.test>", repository.Current.Revision, CancellationToken.None));
        Assert.True(await useCase.ExecuteAsync(" test@example.test ", repository.Current.Revision, CancellationToken.None));
        Assert.Equal("test@example.test", queue.Recipient);
    }
    private sealed class Repository : IEmailSettingsRepository
    {
        public EmailSettingsSnapshot Current = new(true, "smtp.example.test", 587, "", "shop@example.test", "", "https://shop.example.test", true, Guid.NewGuid());
        public UpdateEmailSettingsCommand? Saved;
        public Task<EmailSettingsSnapshot> GetAsync(CancellationToken token) => Task.FromResult(Current);
        public Task<EmailSettingsSnapshot?> SaveAsync(UpdateEmailSettingsCommand command, CancellationToken token) { Saved = command; return Task.FromResult<EmailSettingsSnapshot?>(Current); }
    }
    private sealed class Queue : IEmailQueue
    {
        public string? Recipient;
        public Task EnqueueAsync(string recipient, string subject, string body, CancellationToken token) { Recipient = recipient; return Task.CompletedTask; }
    }
}
