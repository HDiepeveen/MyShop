using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using HDWebFramework.Api.Tests.Security;

namespace HDWebFramework.Api.Tests.Catalog.Seo;

public sealed class WebsiteCurrencyHttpTests
{
    [SecuritySqlFact]
    public async Task ChangingCurrencyReplacesExistingCurrenciesButPreservesAmountsAndOverridesAfterwards()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        async Task<(Guid Id, Guid Variant)> Create(string name, string? currency)
        {
            var typeResponse = await host.Client.PostAsJsonAsync("/api/product-types", new { name = "Type " + name });
            var type = (await typeResponse.Content.ReadFromJsonAsync<JsonObject>())!;
            var response = await host.Client.PostAsJsonAsync("/api/products", new { productTypeId = type["id"]!.GetValue<Guid>(), name, initialVariantName = "Standard" });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var product = (await response.Content.ReadFromJsonAsync<JsonObject>())!;
            var id = product["id"]!.GetValue<Guid>(); var variant = product["initialVariantId"]!.GetValue<Guid>();
            if (currency is not null)
                Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{id}/variants/{variant}/price", new { netAmount = 10, currency, vatRate = 21 })).StatusCode);
            return (id, variant);
        }
        var eur = await Create("Euro", "EUR"); var usd = await Create("Dollar", "USD"); var blank = await Create("No price", null);
        async Task<HDWebFramework.Application.Catalog.Abstractions.ProductSnapshot> Read(Guid id)
        {
            using var scope = host.App.Services.CreateScope();
            return (await scope.ServiceProvider.GetRequiredService<HDWebFramework.Application.Catalog.Abstractions.IProductRepository>()
                .GetByIdAsync(HDWebFramework.Domain.Catalog.ProductId.From(id), CancellationToken.None))!;
        }
        var before = await Read(eur.Id);
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        settings["currency"] = "SRD";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        foreach (var id in new[] { eur.Id, usd.Id })
        {
            var after = await Read(id); var variant = after.Product.Variants.Single();
            Assert.Equal("SRD", variant.Price!.Value.Currency);
            Assert.Equal(12.10m, variant.Price.Value.Amount);
            Assert.Equal(10m, variant.NetPriceAmount); Assert.Equal(21m, variant.VatRate);
        }
        Assert.NotEqual(before.ConcurrencyToken.Revision, (await Read(eur.Id)).ConcurrencyToken.Revision);
        Assert.Null((await Read(blank.Id)).Product.Variants.Single().Price);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync($"/api/products/{usd.Id}/variants/{usd.Variant}/price", new { netAmount = 10, currency = "USD", vatRate = 21 })).StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var current = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        current["footerText"] = "Changed footer";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", current)).StatusCode);
        Assert.Equal("USD", (await Read(usd.Id)).Product.Variants.Single().Price!.Value.Currency);
    }

    [SecuritySqlFact]
    public async Task CurrencyPersistsAndInvalidInputDoesNotChangeIt()
    {
        await using var host = await SecurityHost.Create();
        await host.Csrf(); await host.Login(); await host.Csrf();
        var settings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("EUR", settings["currency"]!.GetValue<string>());
        settings["currency"] = " srd ";
        Assert.Equal(HttpStatusCode.NoContent, (await host.Client.PutAsJsonAsync("/api/seo-settings", settings)).StatusCode);
        var stored = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("SRD", stored["currency"]!.GetValue<string>());
        var publicSettings = (await host.Client.GetFromJsonAsync<JsonObject>("/api/shop/settings"))!;
        Assert.Equal("SRD", publicSettings["currency"]!.GetValue<string>());
        foreach (var invalid in new[] { "", "EU", "123", "EURO" })
        {
            stored["currency"] = invalid;
            Assert.Equal(HttpStatusCode.BadRequest, (await host.Client.PutAsJsonAsync("/api/seo-settings", stored)).StatusCode);
        }
        var unchanged = (await host.Client.GetFromJsonAsync<JsonObject>("/api/seo-settings"))!;
        Assert.Equal("SRD", unchanged["currency"]!.GetValue<string>());
        Assert.Equal(stored["revision"]!.GetValue<string>(), unchanged["revision"]!.GetValue<string>());
    }
}
