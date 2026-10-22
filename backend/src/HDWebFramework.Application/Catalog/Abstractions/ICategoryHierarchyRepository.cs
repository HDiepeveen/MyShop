using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface ICategoryHierarchyRepository
{
    Task<bool> IsDescendantOfAsync(
        CategoryId candidateId,
        CategoryId ancestorId,
        CancellationToken cancellationToken);
}
