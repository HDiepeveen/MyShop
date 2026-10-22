using HDWebFramework.Application.Catalog.Abstractions;

namespace HDWebFramework.Application.Catalog.ListStorefrontCategories;

public sealed class ListStorefrontCategories(IStorefrontCatalog catalog)
{
    private readonly IStorefrontCatalog catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

    public Task<IReadOnlyList<StorefrontCategory>> ExecuteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return catalog.ListCategoriesAsync(cancellationToken);
    }
}
