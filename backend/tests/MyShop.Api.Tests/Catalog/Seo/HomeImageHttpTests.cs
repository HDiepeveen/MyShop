using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;
public sealed class HomeImageHttpTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jZxkAAAAASUVORK5CYII=");
    private static MultipartFormDataContent Upload(Guid revision, byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(revision.ToString()), "revision");
        form.Add(new ByteArrayContent(bytes ?? Png), "file", "icon.png");
        return form;
    }
    [SecuritySqlFact]
    public async Task UploadReplaceAndRemoveAreProtectedAndPreserveHomeText()
    {
        await using var host = await SecurityHost.Create();
        using var guest = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/api/shop/home-image")).StatusCode);
        using (var file = Upload(Guid.NewGuid())) Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/home-image", file)).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        var initial = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        var revision = initial.GetProperty("revision").GetGuid();
        host.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
        await host.Csrf();
        foreach (var bytes in new[] { Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>"), Array.Empty<byte>(), new byte[5 * 1024 * 1024 + 1] })
            using (var file = Upload(revision, bytes)) Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
        var saved = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        Assert.Equal(initial.GetProperty("home").GetRawText(), saved.GetProperty("home").GetRawText());
        var publicSettings = saved;
        var url = publicSettings.GetProperty("homeImageUrl").GetString()!;
        Assert.StartsWith("/api/shop/home-image?v=", url);
        Assert.False(publicSettings.TryGetProperty("homeImageBytes", out _));
        var image = await guest.GetAsync(url);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", saved)).StatusCode);
        Assert.Equal(Png, await guest.GetByteArrayAsync("/api/shop/home-image"));
        var afterSettings = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        var currentRevision = afterSettings.GetProperty("revision").GetGuid();
        using (var file = Upload(currentRevision)) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
        var replaced = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        Assert.NotEqual(saved.GetProperty("homeImageUrl").GetString(), replaced.GetProperty("homeImageUrl").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.SendAsync(new(HttpMethod.Delete, "/api/home-image") { Content = JsonContent.Create(new { revision }) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.SendAsync(new(HttpMethod.Delete, "/api/home-image") { Content = JsonContent.Create(new { revision = replaced.GetProperty("revision").GetGuid() }) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/api/shop/home-image")).StatusCode);
        var removed = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        Assert.Equal(JsonValueKind.Null, removed.GetProperty("homeImageUrl").ValueKind);
        Assert.Equal(initial.GetProperty("home").GetRawText(), removed.GetProperty("home").GetRawText());
        await host.Client.PostAsJsonAsync("/api/auth/logout", new { }); await host.Csrf();
        await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "icon-customer@example.test", password = SecurityHost.Password }); await host.Csrf();
        using (var file = Upload(removed.GetProperty("revision").GetGuid())) Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsync("/api/home-image", file)).StatusCode);
    }
}
