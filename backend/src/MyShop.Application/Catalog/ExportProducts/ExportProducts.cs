using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
namespace MyShop.Application.Catalog.ExportProducts;
public sealed record ExportProductsQuery(ProductTypeId? ProductTypeId = null, CategoryId? CategoryId = null,
    string? Search = null, bool? IsPublished = null, ProductStockFilter? Stock = null);
public sealed class ExportProducts(IProductListRepository repository)
{
    public const int MaximumRows = 1000;
    private readonly IProductListRepository repository = repository ?? throw new ArgumentNullException(nameof(repository));
    public async Task<ProductListPage> ExecuteAsync(ExportProductsQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query); cancellationToken.ThrowIfCancellationRequested();
        if (query.ProductTypeId == default(ProductTypeId) || query.CategoryId == default(CategoryId)) throw new ArgumentException("Filter ID must not be empty.", nameof(query));
        if (query.Stock is { } stock && !Enum.IsDefined(stock)) throw new ArgumentOutOfRangeException(nameof(query.Stock));
        if (query.Search is not null && string.IsNullOrWhiteSpace(query.Search)) throw new ArgumentException("Search must not be empty.", nameof(query.Search));
        var page = await repository.ListAsync(0, MaximumRows, query.ProductTypeId, query.CategoryId, query.Search?.Trim(), cancellationToken, query.IsPublished, query.Stock);
        return page with { Items = page.Items.Take(MaximumRows).ToArray() };
    }
}
