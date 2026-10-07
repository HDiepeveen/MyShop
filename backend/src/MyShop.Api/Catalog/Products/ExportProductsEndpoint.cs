using System.Globalization;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.ExportProducts.ExportProducts;
namespace MyShop.Api.Catalog.Products;
public static class ExportProductsEndpoint
{
    public static IEndpointRouteBuilder MapExportProducts(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/products/export", ExecuteAsync).WithName("ExportProducts");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync([FromQuery] Guid? productTypeId, [FromQuery] Guid? categoryId,
        [FromQuery] string? search, [FromQuery] bool? published, [FromQuery] string? stock,
        [FromServices] UseCase useCase, HttpContext context, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            ProductStockFilter? stockFilter = stock switch { null or "" => null, "low" => ProductStockFilter.Low,
                "out" => ProductStockFilter.OutOfStock, "untracked" => ProductStockFilter.Untracked,
                _ => throw new ArgumentException("Unknown stock filter.") };
            var page = await useCase.ExecuteAsync(new(productTypeId is null ? null : ProductTypeId.From(productTypeId.Value),
                categoryId is null ? null : CategoryId.From(categoryId.Value), search, published, stockFilter), cancellationToken);
            var csv = new StringBuilder("ProductId,ProductTypeId,Name,VariantCount,PublicationStatus\r\n");
            foreach (var item in page.Items)
            {
                cancellationToken.ThrowIfCancellationRequested();
                csv.Append(Cell(item.Id.ToString("D"))).Append(',').Append(Cell(item.ProductTypeId.ToString("D"))).Append(',')
                    .Append(Cell(item.Name)).Append(',').Append(Cell(item.VariantCount.ToString(CultureInfo.InvariantCulture))).Append(',')
                    .Append(Cell(item.IsPublished ? "published" : "draft")).Append("\r\n");
            }
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Headers["X-Export-Limit"] = UseCase.MaximumRows.ToString(CultureInfo.InvariantCulture);
            context.Response.Headers["X-Export-Truncated"] = page.TotalCount > page.Items.Count ? "true" : "false";
            return Results.File(Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray(), "text/csv; charset=utf-8", "myshop-products.csv");
        }
        catch (ArgumentException error) { return Results.BadRequest(new ProblemDetails { Title = "Invalid product export", Detail = error.Message }); }
    }
    private static string Cell(string value)
    {
        var index = 0;
        while (index < value.Length && (char.IsWhiteSpace(value[index]) || value[index] is '\uFEFF' or '\u200B')) index++;
        if ((index < value.Length && value[index] is '=' or '+' or '-' or '@') || (value.Length > 0 && char.IsControl(value[0]))) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
    }
}
