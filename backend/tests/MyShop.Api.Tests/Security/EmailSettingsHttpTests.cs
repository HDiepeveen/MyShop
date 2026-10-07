using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
namespace MyShop.Api.Tests.Security;
[Collection("SqlServer integration")]
public sealed class EmailSettingsHttpTests
{
    [Fact]
    public async Task Only_administrators_can_read_save_or_test_settings()
    {
        await using var host = await SecurityHost.Create();
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Client.GetAsync("/api/email-settings")).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "mail-settings-customer@example.test", password = SecurityHost.Password })).StatusCode);
        await host.Csrf();
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.GetAsync("/api/email-settings")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PutAsJsonAsync("/api/email-settings", new { })).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsJsonAsync("/api/email-settings/test", new { })).StatusCode);
    }
    [Fact]
    public async Task Settings_hide_passwords_preserve_revisions_and_queue_tests_with_saved_configuration()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
        var before = await host.Client.GetFromJsonAsync<JsonElement>("/api/email-settings");
        var revision = before.GetProperty("revision").GetGuid();
        var body = new { enabled = true, host = "smtp.example.test", port = 587, userName = "user", fromAddress = "shop@example.test", fromName = "MyShop", publicBaseUrl = "https://shop.example.test", password = "test-only-not-a-real-password", clearPassword = false, revision };
        var response = await host.Client.PutAsJsonAsync("/api/email-settings", body);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(body.password, json);
        var saved = JsonSerializer.Deserialize<JsonElement>(json);
        Assert.True(saved.GetProperty("passwordConfigured").GetBoolean());
        Assert.False(saved.TryGetProperty("password", out _)); Assert.False(saved.TryGetProperty("protectedPassword", out _));
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/email-settings", body)).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsJsonAsync("/api/email-settings/test", new { recipient = "test@example.test", revision })).StatusCode);
        var current = saved.GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.Accepted, (await host.Client.PostAsJsonAsync("/api/email-settings/test", new { recipient = "test@example.test", revision = current })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsJsonAsync("/api/email-settings/test", new { recipient = "Victim <test@example.test>", revision = current })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/email-settings", new { body.enabled, host = "https://smtp.example.test", body.port, body.userName, body.fromAddress, body.fromName, body.publicBaseUrl, password = "", clearPassword = false, revision = current })).StatusCode);
        var stored = await host.Client.GetFromJsonAsync<JsonElement>("/api/email-settings");
        Assert.Equal("smtp.example.test", stored.GetProperty("host").GetString());
        Assert.Equal(current, stored.GetProperty("revision").GetGuid());
    }
}
