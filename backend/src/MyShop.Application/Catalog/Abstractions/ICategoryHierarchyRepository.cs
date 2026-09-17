using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog.Abstractions;

public interface ICategoryHierarchyRepository
{
    Task<bool> IsDescendantOfAsync(
        CategoryId candidateId,
        CategoryId ancestorId,
        CancellationToken cancellationToken);
}
