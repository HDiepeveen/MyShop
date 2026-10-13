using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;
public sealed class ShopIconHttpTests
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
    public async Task UploadReplaceAndRemoveAreProtectedAndPreserveColors()
    {
        await using var host = await SecurityHost.Create();
        using var guest = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/api/shop/icon")).StatusCode);
        using (var file = Upload(Guid.NewGuid())) Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/shop-icon", file)).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        var initial = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        var revision = initial.GetProperty("revision").GetGuid();
        host.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
        await host.Csrf();
        foreach (var bytes in new[] { Encoding.UTF8.GetBytes("<svg onload='alert(1)'/>"), Array.Empty<byte>(), new byte[1024 * 1024 + 1] })
            using (var file = Upload(revision, bytes)) Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
        using (var file = Upload(revision)) Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
        var saved = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        Assert.Equal(initial.GetProperty("colors").GetRawText(), saved.GetProperty("colors").GetRawText());
        var publicSettings = await guest.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        var url = publicSettings.GetProperty("iconUrl").GetString()!;
        Assert.StartsWith("/api/shop/icon?v=", url);
        Assert.False(publicSettings.TryGetProperty("iconBytes", out _));
        var image = await guest.GetAsync(url);
        Assert.Equal("image/png", image.Content.Headers.ContentType!.MediaType);
        Assert.Equal(Png, await image.Content.ReadAsByteArrayAsync());
        Assert.Equal("nosniff", image.Headers.GetValues("X-Content-Type-Options").Single());
        var colorSave = new { colors = saved.GetProperty("colors"), revision = saved.GetProperty("revision").GetGuid() };
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/shop-appearance", colorSave)).StatusCode);
        Assert.Equal(Png, await guest.GetByteArrayAsync("/api/shop/icon"));
        var afterColors = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        var currentRevision = afterColors.GetProperty("revision").GetGuid();
        using (var file = Upload(currentRevision)) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
        var replaced = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        Assert.NotEqual(saved.GetProperty("iconUrl").GetString(), replaced.GetProperty("iconUrl").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.SendAsync(new(HttpMethod.Delete, "/api/shop-icon") { Content = JsonContent.Create(new { revision }) })).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.SendAsync(new(HttpMethod.Delete, "/api/shop-icon") { Content = JsonContent.Create(new { revision = replaced.GetProperty("revision").GetGuid() }) })).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync("/api/shop/icon")).StatusCode);
        var removed = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop-appearance");
        Assert.Equal(JsonValueKind.Null, removed.GetProperty("iconUrl").ValueKind);
        Assert.Equal(initial.GetProperty("colors").GetRawText(), removed.GetProperty("colors").GetRawText());
        await host.Client.PostAsJsonAsync("/api/auth/logout", new { }); await host.Csrf();
        await host.Client.PostAsJsonAsync("/api/customer/auth/register", new { email = "icon-customer@example.test", password = SecurityHost.Password }); await host.Csrf();
        using (var file = Upload(removed.GetProperty("revision").GetGuid())) Assert.Equal(HttpStatusCode.Forbidden, (await host.Client.PostAsync("/api/shop-icon", file)).StatusCode);
    }
}
