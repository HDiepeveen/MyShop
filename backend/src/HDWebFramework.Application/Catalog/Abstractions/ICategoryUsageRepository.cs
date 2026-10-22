using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Catalog.Abstractions;

public interface ICategoryUsageRepository
{
    Task<CategoryUsage> GetUsageAsync(CategoryId categoryId, CancellationToken cancellationToken);
}

public sealed record CategoryUsage(int DirectChildCount, int ProductAssignmentCount)
{
    public bool IsInUse => DirectChildCount > 0 || ProductAssignmentCount > 0;
}
