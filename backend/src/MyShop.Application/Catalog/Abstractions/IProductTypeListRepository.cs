namespace MyShop.Application.Catalog.Abstractions;

public interface IProductTypeListRepository
{
    Task<IReadOnlyList<ProductTypeListItem>> ListAsync(
        string? searchTerm,
        CancellationToken cancellationToken);
}

public sealed record ProductTypeListItem(Guid Id, string Name);
