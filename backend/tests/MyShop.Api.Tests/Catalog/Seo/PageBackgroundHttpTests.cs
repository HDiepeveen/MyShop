using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using MyShop.Api.Tests.Security;
namespace MyShop.Api.Tests.Catalog.Seo;
public sealed class PageBackgroundHttpTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jZxkAAAAASUVORK5CYII=");
    private static MultipartFormDataContent Upload(string revision, byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent(); form.Add(new StringContent(revision), "revision");
        form.Add(new ByteArrayContent(bytes ?? Png), "file", "photo.png"); return form;
    }
    [SecuritySqlFact]
    public async Task Photos_follow_pages_through_rename_and_reorder_and_are_removed_with_the_page()
    {
        await using var host = await SecurityHost.Create();
        using var guest = new HttpClient { BaseAddress = host.Client.BaseAddress };
        using (var form = Upload(Guid.NewGuid().ToString())) Assert.Equal(HttpStatusCode.Unauthorized, (await guest.PostAsync("/api/page-images/" + Guid.NewGuid(), form)).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        async Task<JsonObject> Settings() => (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        var settings = await Settings();
        settings["home"] = JsonNode.Parse("""{"enabled":true,"heading":"Home","imageMode":"cover","items":[{"title":"APK","description":"Keuring","imageMode":"cover"},{"title":"Reparaties","description":"Reparatie"}]}""");
        settings["company"]!["imageMode"] = "cover";
        settings["assortmentImageMode"] = "cover";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        settings = await Settings(); var id = settings["home"]!["items"]![0]!["pageId"]!.GetValue<string>();
        var path = "/api/page-images/" + id; var revision = settings["revision"]!.GetValue<string>();
        using (var form = Upload(revision, [1,2,3])) Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync(path, form)).StatusCode);
        using (var form = Upload(revision)) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync(path, form)).StatusCode);
        using (var form = Upload(revision)) Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsync(path, form)).StatusCode);
        settings = await Settings();
        var imageUrl = settings["home"]!["items"]![0]!["imageUrl"]!.GetValue<string>();
        Assert.Equal(Png, await guest.GetByteArrayAsync(imageUrl));
        var items = settings["home"]!["items"]!.AsArray(); var page = items[0]!.DeepClone(); page["title"] = "APK keuring"; items.RemoveAt(0); items.Add(page);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var publicPage = (await guest.GetFromJsonAsync<JsonObject>("/api/shop/pages/apk-keuring"))!;
        Assert.Equal(id, publicPage["pageId"]!.GetValue<string>()); Assert.Equal("cover", publicPage["imageMode"]!.GetValue<string>());
        Assert.Equal(Png, await guest.GetByteArrayAsync(publicPage["imageUrl"]!.GetValue<string>()));
        settings = await Settings();
        using (var form = Upload(settings["revision"]!.GetValue<string>())) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/page-images/ac8107f2-a136-4b4f-9e85-b70f14860701", form)).StatusCode);
        settings = await Settings();
        using (var form = Upload(settings["revision"]!.GetValue<string>())) Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync("/api/page-images/d2f06ded-68a2-4936-9880-327d616d1a42", form)).StatusCode);
        var shop = (await guest.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("cover",shop["assortmentImageMode"]!.GetValue<string>());
        Assert.Equal(Png,await guest.GetByteArrayAsync(shop["assortmentImageUrl"]!.GetValue<string>()));
        var contact = (await guest.GetFromJsonAsync<JsonObject>("/api/shop/company"))!;
        Assert.Equal("cover", contact["imageMode"]!.GetValue<string>()); Assert.Equal(Png, await guest.GetByteArrayAsync(contact["imageUrl"]!.GetValue<string>()));
        settings = await Settings(); settings["home"]!["items"]!.AsArray().RemoveAt(1);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await guest.GetAsync(imageUrl)).StatusCode);
        settings = await Settings(); settings["home"]!["imageMode"]="invalid";
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings",settings)).StatusCode);
        settings=await Settings();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.SendAsync(new(HttpMethod.Delete,"/api/page-images/ac8107f2-a136-4b4f-9e85-b70f14860701"){Content=JsonContent.Create(new {revision=settings["revision"]!.GetValue<string>()})})).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound,(await guest.GetAsync("/api/shop/page-images/ac8107f2-a136-4b4f-9e85-b70f14860701")).StatusCode);
    }
}
