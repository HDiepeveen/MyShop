using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MyShop.Api.Tests.Security;

namespace MyShop.Api.Tests.Catalog.Seo;
public sealed class CatalogSeoHttpTests
{
    [SecuritySqlFact]
    public async Task Type_headings_apply_to_every_product_of_that_type_and_survive_renaming()
    {
        await using var host = await SecurityHost.Create();
        using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
        var missing = $"/api/product-types/{Guid.NewGuid()}/section-headings";
        Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync(missing)).StatusCode);
        await host.Csrf(); await host.Login(); await host.Csrf();
        Assert.Equal(HttpStatusCode.NotFound, (await host.Client.GetAsync(missing)).StatusCode);
        var first = await CreateProduct(host, "First car");
        var typeId = first.GetProperty("productTypeId").GetGuid();
        var secondResponse = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = typeId, name = "Second car" });
        Assert.Equal(HttpStatusCode.Created, secondResponse.StatusCode);
        var second = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();
        var other = await CreateProduct(host, "Other type");
        var settings = await host.Client.GetFromJsonAsync<JsonElement>($"/api/product-types/{typeId}/section-headings");
        var request = new { aboutHeading = "Over deze auto", attributesHeading = "Voertuiggegevens", revision = settings.GetProperty("revision").GetGuid() };
        var productRevision = (await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{first.GetProperty("id").GetGuid()}/seo")).GetProperty("revision").GetGuid();
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", request)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PatchAsJsonAsync($"/api/product-types/{typeId}/name", new { name = "Cars" })).StatusCode);
        foreach (var product in new[] { first, second })
        {
            var info = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{product.GetProperty("id").GetGuid()}/seo");
            Assert.Equal("Over deze auto", info.GetProperty("resolvedAboutHeading").GetString());
            Assert.Equal("Voertuiggegevens", info.GetProperty("resolvedAttributesHeading").GetString());
            Assert.False(info.GetProperty("values").TryGetProperty("aboutHeading", out _));
            Assert.Equal(productRevision, info.GetProperty("revision").GetGuid());
        }
        var otherInfo = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{other.GetProperty("id").GetGuid()}/seo");
        Assert.Equal("Over dit product", otherInfo.GetProperty("resolvedAboutHeading").GetString());
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", request)).StatusCode);
    }

    [SecuritySqlFact]
    public async Task Branding_defaults_and_validation_preserve_stored_settings()
    {
        await using var host = await SecurityHost.Create();
        var initial = await host.Client.GetFromJsonAsync<JsonElement>("/api/shop/settings");
        Assert.Equal("MyShop", initial.GetProperty("shopName").GetString());
        Assert.Equal("Welkom bij MyShop", initial.GetProperty("welcomeText").GetString());
        Assert.Equal("Bekijk onze producten en kies de variant die bij je past.", initial.GetProperty("introduction").GetString());
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        var revision = settings.GetProperty("revision").GetGuid();
        foreach (var (name, welcome, introduction) in new[]
        {
            (" ", "Welcome", "Introduction"), (new string('n', 101), "Welcome", "Introduction"),
            ("Company", " ", "Introduction"), ("Company", new string('w', 201), "Introduction"),
            ("Company", "Welcome", " "), ("Company", "Welcome", new string('i', 1001))
        })
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings",
                new { heading = "Heading", seoTitle = "Title", shopName = name, welcomeText = welcome, introduction, revision })).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", new { heading = "Heading", seoTitle = "Title", revision })).StatusCode);
        var unchanged = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
        Assert.Equal(revision, unchanged.GetProperty("revision").GetGuid());
        Assert.Equal("MyShop", unchanged.GetProperty("shopName").GetString());
    }

    [SecuritySqlFact]
    public async Task Settings_optional_product_metadata_and_historical_addresses_work_without_exposing_drafts_or_admin_pages()
    {
        var folder = Path.Combine(Path.GetTempPath(), "MyShopSeo_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>MyShop</title></head><body><app-root></app-root></body></html>");
        try
        {
            await using var host = await SecurityHost.Create(new() { ["Hosting:FrontendRoot"] = folder });
            using var visitor = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = host.Client.BaseAddress };
            Assert.Equal(HttpStatusCode.Unauthorized, (await visitor.GetAsync("/api/seo-settings")).StatusCode);
            await host.Csrf(); Assert.Equal(HttpStatusCode.NoContent, (await host.Login()).StatusCode); await host.Csrf();
            var settings = await host.Client.GetFromJsonAsync<JsonElement>("/api/seo-settings");
            var request = new { shopName = "Autohuis Hans", welcomeText = "Welkom bij Autohuis Hans", introduction = "Bekijk onze occasions.", heading = "Occasions te koop", seoTitle = "Auto kopen $& <script>bad</script>", revision = settings.GetProperty("revision").GetGuid() };
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", request)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", request)).StatusCode);
            var page = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products");
            Assert.Equal("Occasions te koop", page.GetProperty("heading").GetString());
            var html = await visitor.GetStringAsync("/winkel");
            Assert.Contains("<h1>Occasions te koop</h1>", html);
            Assert.Contains("Welkom bij Autohuis Hans", html);
            Assert.Contains("Bekijk onze occasions.", html);
            var branding = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/settings");
            Assert.Equal("Autohuis Hans", branding.GetProperty("shopName").GetString());
            Assert.Equal(new[] { "introduction", "shopName", "welcomeText" }, branding.EnumerateObject().Select(p => p.Name).Order().ToArray());
            Assert.Contains("$&amp;", html);
            Assert.DoesNotContain("<script>bad</script>", html);
            var product = await CreateProduct(host, "Opel Corsa 2014");
            var id = product.GetProperty("id").GetGuid();
            var typeId = product.GetProperty("productTypeId").GetGuid();
            var typeSettings = await host.Client.GetFromJsonAsync<JsonElement>($"/api/product-types/{typeId}/section-headings");
            var typeRequest = new { aboutHeading = " Over deze auto ", attributesHeading = "Voertuiggegevens", revision = typeSettings.GetProperty("revision").GetGuid() };
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", typeRequest)).StatusCode);
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", typeRequest)).StatusCode);
            var details = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
            var revision = details.GetProperty("revision").GetGuid();
            var initial = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}/seo");
            Assert.Equal(Guid.Empty, initial.GetProperty("revision").GetGuid());
            var auto = initial.GetProperty("resolvedAddress").GetString();
            Assert.StartsWith("opel-corsa-2014-", auto);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/seo", new { seoTitle = "Opel Corsa kopen", seoDescription = "Bekijk deze Opel Corsa.", webAddress = "opel-corsa-2014", revision = Guid.Empty })).StatusCode);
            Assert.Equal(revision, (await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}")).GetProperty("revision").GetGuid());
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/winkel/opel-corsa-2014")).StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, (await visitor.GetAsync("/api/shop/products/opel-corsa-2014")).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/presentation", new { description = "Een goed onderhouden auto.", imageUrl = "https://example.test/corsa.jpg", imageAlt = "Opel Corsa", isPublished = true, revision })).StatusCode);
            var publicProduct = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products/opel-corsa-2014");
            Assert.Equal(id, publicProduct.GetProperty("id").GetGuid());
            Assert.Equal("Opel Corsa kopen", publicProduct.GetProperty("seoTitle").GetString());
            Assert.Equal("Over deze auto", publicProduct.GetProperty("aboutHeading").GetString());
            Assert.Equal("Voertuiggegevens", publicProduct.GetProperty("attributesHeading").GetString());
            Assert.Equal(HttpStatusCode.OK, (await visitor.GetAsync("/api/shop/products/opel-corsa-2014/prices")).StatusCode);
            html = await visitor.GetStringAsync("/winkel/opel-corsa-2014");
            Assert.Contains("<title>Opel Corsa kopen</title>", html);
            Assert.Contains("name=\"description\" content=\"Bekijk deze Opel Corsa.\"", html);
            Assert.Contains("<h1>Opel Corsa 2014</h1>", html);
            Assert.Contains("<h2>Over deze auto</h2>", html);
            Assert.DoesNotContain("noindex", html);
            Assert.Equal(HttpStatusCode.MovedPermanently, (await visitor.GetAsync($"/winkel/{id}")).StatusCode);
            var current = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}/seo");
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync($"/api/products/{id}/seo", new { webAddress = "another", revision = Guid.Empty })).StatusCode);
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/seo", new { webAddress = "corsa-lpg", revision = current.GetProperty("revision").GetGuid() })).StatusCode);
            var old = await visitor.GetAsync("/winkel/opel-corsa-2014?search=auto");
            Assert.Equal(HttpStatusCode.MovedPermanently, old.StatusCode);
            Assert.Equal("/winkel/corsa-lpg?search=auto", old.Headers.Location!.ToString());
            var oldAutomatic = await visitor.GetAsync("/winkel/" + auto);
            Assert.Equal(HttpStatusCode.MovedPermanently, oldAutomatic.StatusCode);
            typeSettings = await host.Client.GetFromJsonAsync<JsonElement>($"/api/product-types/{typeId}/section-headings");
            Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", new { aboutHeading = " ", attributesHeading = (string?)null, revision = typeSettings.GetProperty("revision").GetGuid() })).StatusCode);
            publicProduct = await visitor.GetFromJsonAsync<JsonElement>("/api/shop/products/corsa-lpg");
            Assert.Equal("Over dit product", publicProduct.GetProperty("aboutHeading").GetString());
            Assert.Equal("Productkenmerken", publicProduct.GetProperty("attributesHeading").GetString());
            Assert.Equal("Opel Corsa 2014 · Autohuis Hans", publicProduct.GetProperty("seoTitle").GetString());
            Assert.Equal("Een goed onderhouden auto.", publicProduct.GetProperty("seoDescription").GetString());
            var unchanged = await host.Client.GetFromJsonAsync<JsonElement>($"/api/product-types/{typeId}/section-headings");
            foreach (var headings in new[] { new { aboutHeading = new string('a', 201), attributesHeading = "Valid" }, new { aboutHeading = "Valid", attributesHeading = new string('a', 201) } })
                Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync($"/api/product-types/{typeId}/section-headings", new { headings.aboutHeading, headings.attributesHeading, revision = unchanged.GetProperty("revision").GetGuid() })).StatusCode);
            Assert.Equal(unchanged.GetProperty("revision").GetGuid(), (await host.Client.GetFromJsonAsync<JsonElement>($"/api/product-types/{typeId}/section-headings")).GetProperty("revision").GetGuid());
            var other = await CreateProduct(host, "Another car");
            Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync($"/api/products/{other.GetProperty("id").GetGuid()}/seo", new { webAddress = "opel-corsa-2014", revision = Guid.Empty })).StatusCode);
            current = await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}/seo");
            foreach (var invalid in new[] { "account", "betaling", "a/b", "https://example.test", "a--b" })
                Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync($"/api/products/{id}/seo", new { webAddress = invalid, revision = current.GetProperty("revision").GetGuid() })).StatusCode);
            Assert.Equal(current.GetProperty("revision").GetGuid(), (await host.Client.GetFromJsonAsync<JsonElement>($"/api/products/{id}/seo")).GetProperty("revision").GetGuid());
            var privatePage = await visitor.GetAsync("/inloggen");
            Assert.Contains("noindex", await privatePage.Content.ReadAsStringAsync());
            Assert.Contains("noindex", privatePage.Headers.GetValues("X-Robots-Tag"));
        }
        finally { Directory.Delete(folder, true); }
    }
    [SecuritySqlFact]
    public async Task Concurrent_products_cannot_claim_the_same_address()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var first = await CreateProduct(host, "First");
        var second = await CreateProduct(host, "Second");
        var responses = await Task.WhenAll(new[] { first, second }.Select(product => host.Client.PutAsJsonAsync(
            $"/api/products/{product.GetProperty("id").GetGuid()}/seo", new { webAddress = "shared-address", revision = Guid.Empty })));
        Assert.Equal(new[] { 204, 409 }, responses.Select(response => (int)response.StatusCode).Order().ToArray());
        var infos = await Task.WhenAll(new[] { first, second }.Select(product => host.Client.GetFromJsonAsync<JsonElement>(
            $"/api/products/{product.GetProperty("id").GetGuid()}/seo")));
        Assert.Single(infos, info => info.GetProperty("values").GetProperty("webAddress").GetString() == "shared-address");
    }

    private static async Task<JsonElement> CreateProduct(SecurityHost host, string name)
    {
        var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Type " + Guid.NewGuid() });
        Assert.Equal(HttpStatusCode.Created, typeResponse.StatusCode);
        var type = await typeResponse.Content.ReadFromJsonAsync<JsonElement>();
        var response = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type.GetProperty("id").GetGuid(), name, initialVariantName = "Standard" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
