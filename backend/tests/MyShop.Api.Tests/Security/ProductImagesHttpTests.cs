using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MyShop.Api.Tests.Security;

[Collection("SqlServer integration")]
public sealed class ProductImagesHttpTests
{
    private static readonly byte[] Png = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jZxkAAAAASUVORK5CYII=");
    private static MultipartFormDataContent Upload(Guid revision, int count = 1, byte[]? bytes = null)
    {
        var form = new MultipartFormDataContent();
        form.Add(new StringContent(revision.ToString()), "revision");
        form.Add(new StringContent("Front of shirt"), "alternativeText");
        for (var i = 0; i < count; i++) form.Add(new ByteArrayContent(bytes ?? Png), "files", $"shirt-{i}.png");
        return form;
    }
    private static async Task<(Guid Id, Guid Revision)> Product(SecurityHost host)
    {
        var type = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Shirt" });
        Assert.Equal(HttpStatusCode.Created, type.StatusCode);
        var typeId = (await type.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        var product = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = typeId, name = "Cotton shirt", initialVariantName = "Blue / S" });
        Assert.Equal(HttpStatusCode.Created, product.StatusCode);
        var data = await product.Content.ReadFromJsonAsync<JsonElement>();
        return (data.GetProperty("id").GetGuid(), data.GetProperty("revision").GetGuid());
    }

    [SecuritySqlFact]
    public async Task Upload_publish_select_and_delete_preserve_gallery_and_protect_drafts()
    {
        await using var host = await SecurityHost.Create(); await host.Csrf(); await host.Login(); await host.Csrf();
        var product = await Product(host);
        using var upload = Upload(product.Revision, 2);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync($"/api/products/{product.Id}/images", upload)).StatusCode);
        var gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        var images = gallery.GetProperty("images").EnumerateArray().ToArray(); Assert.Equal(2, images.Length);
        var firstUrl = images[0].GetProperty("url").GetString()!;
        using var anonymous = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync(firstUrl)).StatusCode);
        Assert.Equal(Png, await host.Client.GetByteArrayAsync(firstUrl));
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{product.Id}/presentation", new {
            description = "Cotton shirt", imageUrl = firstUrl, imageAlt = "Blue shirt", isPublished = true,
            revision = gallery.GetProperty("revision").GetGuid() })).StatusCode);
        var content = await anonymous.GetAsync(firstUrl); Assert.Equal(HttpStatusCode.OK, content.StatusCode);
        Assert.Equal("image/png", content.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", content.Headers.GetValues("X-Content-Type-Options").Single());
        var storefront = await anonymous.GetFromJsonAsync<JsonElement>($"/api/shop/products/{product.Id}");
        Assert.Equal(2, storefront.GetProperty("images").GetArrayLength());
        Assert.DoesNotContain("bytes", storefront.ToString(), StringComparison.OrdinalIgnoreCase);
        var secondId = images[1].GetProperty("id").GetGuid();
        gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        var revision = gallery.GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{product.Id}/images/{secondId}/main", new { revision })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync($"/api/products/{product.Id}/images/{secondId}/main", new { revision })).StatusCode);
        foreach (var image in images.Reverse())
        {
            gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
            using var request = new HttpRequestMessage(HttpMethod.Delete, $"/api/products/{product.Id}/images/{image.GetProperty("id").GetGuid()}")
                { Content = JsonContent.Create(new { revision = gallery.GetProperty("revision").GetGuid() }) };
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.SendAsync(request)).StatusCode);
        }
        Assert.Equal(HttpStatusCode.NotFound, (await anonymous.GetAsync($"/api/shop/products/{product.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync(firstUrl)).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Concurrent_uploads_cannot_exceed_limit_and_product_deletion_removes_bytes()
    {
        await using var host = await SecurityHost.Create(); await host.Csrf(); await host.Login(); await host.Csrf();
        var product = await Product(host);
        using (var initial = Upload(product.Revision, 9))
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync($"/api/products/{product.Id}/images", initial)).StatusCode);
        var gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        using var first = Upload(gallery.GetProperty("revision").GetGuid());
        using var second = Upload(gallery.GetProperty("revision").GetGuid());
        var responses = await Task.WhenAll(host.Client.PostAsync($"/api/products/{product.Id}/images", first),
            host.Client.PostAsync($"/api/products/{product.Id}/images", second));
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.NoContent);
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
        gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        Assert.Equal(10, gallery.GetProperty("images").GetArrayLength());
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.DeleteAsync($"/api/products/{product.Id}")).StatusCode);
        foreach (var image in gallery.GetProperty("images").EnumerateArray())
            Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync(image.GetProperty("url").GetString())).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Upload_rejects_invalid_files_stale_revision_wrong_owner_and_overflow_atomically()
    {
        await using var host = await SecurityHost.Create(); await host.Csrf(); await host.Login(); await host.Csrf();
        var product = await Product(host); var other = await Product(host);
        using (var invalid = Upload(product.Revision, 2, "<svg/>"u8.ToArray()))
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync($"/api/products/{product.Id}/images", invalid)).StatusCode);
        using (var valid = Upload(product.Revision, 10))
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PostAsync($"/api/products/{product.Id}/images", valid)).StatusCode);
        using (var stale = Upload(product.Revision))
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PostAsync($"/api/products/{product.Id}/images", stale)).StatusCode);
        var gallery = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        using (var overflow = Upload(gallery.GetProperty("revision").GetGuid()))
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync($"/api/products/{product.Id}/images", overflow)).StatusCode);
        var unchanged = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.Id}/images");
        Assert.Equal(gallery.GetProperty("revision").GetGuid(), unchanged.GetProperty("revision").GetGuid());
        Assert.Equal(10, unchanged.GetProperty("images").GetArrayLength());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync($"/api/products/{other.Id}/presentation", new {
            description = "Other", imageUrl = gallery.GetProperty("images")[0].GetProperty("url").GetString(), imageAlt = "Other", isPublished = true, revision = other.Revision })).StatusCode);
        using var anonymous = new HttpClient { BaseAddress = host.Client.BaseAddress };
        Assert.Equal(HttpStatusCode.Unauthorized, (await anonymous.GetAsync($"/api/products/{product.Id}/images")).StatusCode);
        host.Client.DefaultRequestHeaders.Remove("X-XSRF-TOKEN");
        using var noCsrf = Upload(unchanged.GetProperty("revision").GetGuid());
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PostAsync($"/api/products/{product.Id}/images", noCsrf)).StatusCode);
    }
}
