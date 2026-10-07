using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using MyShop.Api.Catalog.Products;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ExportProducts.ExportProducts;
namespace MyShop.Api.Tests.Catalog.Products;
public sealed class ExportProductsEndpointTests
{
    [Theory]
    [InlineData("=SUM(1,2)")]
    [InlineData(" +SUM(1,2)")]
    [InlineData("-value")]
    [InlineData("@value")]
    [InlineData("\tvalue")]
    [InlineData("\uFEFF=SUM(1,2)")]
    [InlineData("\u200B=SUM(1,2)")]
    public async Task NeutralizesSpreadsheetPrefixes(string name)
    {
        var repo = new Repository { Name = name }; var result = await ExportProductsEndpoint.ExecuteAsync(null, null, null, null, null, new UseCase(repo), new DefaultHttpContext(), default);
        var file = Assert.IsType<FileContentHttpResult>(result); var csv = Encoding.UTF8.GetString(file.FileContents.ToArray());
        Assert.Contains("\"'" + name.Replace("\"", "\"\"") + "\"", csv);
    }
    [Fact]
    public async Task QuotesNamesAndReturnsBomHeadersAndDownloadMetadata()
    {
        var repo = new Repository { Name = "Shirt, \"linen\"\r\nBlue", Total = 1001 }; var context = new DefaultHttpContext();
        var result = await ExportProductsEndpoint.ExecuteAsync(null, null, " Shirt ", false, "untracked", new UseCase(repo), context, default);
        var file = Assert.IsType<FileContentHttpResult>(result); var bytes = file.FileContents.ToArray(); var csv = Encoding.UTF8.GetString(bytes);
        Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, bytes[..3]); Assert.Contains("\"Shirt, \"\"linen\"\"\r\nBlue\"", csv);
        Assert.Equal("myshop-products.csv", file.FileDownloadName); Assert.Equal("text/csv; charset=utf-8", file.ContentType);
        Assert.Equal("1000", context.Response.Headers["X-Export-Limit"].ToString()); Assert.Equal("true", context.Response.Headers["X-Export-Truncated"].ToString());
        Assert.Equal("no-store", context.Response.Headers.CacheControl.ToString()); Assert.Equal("Shirt", repo.Search); Assert.False(repo.Published); Assert.Equal(ProductStockFilter.Untracked, repo.Stock); Assert.Equal(1000, repo.Limit);
    }
    [Fact]
    public async Task RejectsInvalidFiltersBeforeReading()
    {
        var repo = new Repository(); var result = await ExportProductsEndpoint.ExecuteAsync(null, null, null, null, "bad", new UseCase(repo), new DefaultHttpContext(), default);
        Assert.IsType<BadRequest<Microsoft.AspNetCore.Mvc.ProblemDetails>>(result); Assert.Equal(0, repo.Calls);
    }
    private sealed class Repository : IProductListRepository
    {
        public string Name = "Product"; public int Total = 1, Limit, Calls; public string? Search; public bool? Published; public ProductStockFilter? Stock;
        public Task<ProductListPage> ListAsync(int offset, int limit, ProductTypeId? productTypeId, CategoryId? categoryId, string? searchTerm, CancellationToken cancellationToken, bool? isPublished = null, ProductStockFilter? stock = null)
        { Calls++; Limit = limit; Search = searchTerm; Published = isPublished; Stock = stock; return Task.FromResult(new ProductListPage([new(Guid.NewGuid(), Guid.NewGuid(), Name, 2, false)], Total)); }
    }
}
