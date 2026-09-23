using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository
    : ICategoryRepository, ICategoryListRepository, ICategoryWriter,
      ICategoryHierarchyRepository, ICategoryUsageRepository
{
    private readonly MyShopDbContext _dbContext;

    internal CategoryRepository(MyShopDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<Category?> GetByIdAsync(
        CategoryId id,
        CancellationToken cancellationToken)
    {
        var persistence = await _dbContext.Categories
            .AsNoTracking()
            .SingleOrDefaultAsync(category => category.Id == id.Value, cancellationToken);

        return persistence is null
            ? null
            : CategoryPersistenceMapper.ToDomain(persistence);
    }

    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);
        var persistence = new CategoryPersistence { Id = category.Id.Value };
        CategoryPersistenceWriter.Write(category, persistence);
        _dbContext.Categories.Add(persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task SaveAsync(Category category, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(category);
        var persistence = await _dbContext.Categories.SingleOrDefaultAsync(
            row => row.Id == category.Id.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Category '{category.Id}' no longer exists.");
        CategoryPersistenceWriter.Write(category, persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(CategoryId categoryId, CancellationToken cancellationToken)
    {
        var persistence = await _dbContext.Categories.SingleOrDefaultAsync(
            row => row.Id == categoryId.Value, cancellationToken)
            ?? throw new InvalidOperationException($"Category '{categoryId}' no longer exists.");
        _dbContext.Categories.Remove(persistence);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> IsDescendantOfAsync(
        CategoryId candidateId,
        CategoryId ancestorId,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>();
        Guid? currentId = candidateId.Value;
        while (currentId is not null && visited.Add(currentId.Value))
        {
            if (currentId.Value == ancestorId.Value)
                return true;
            currentId = await ParentIdQuery(_dbContext.Categories, currentId.Value)
                .SingleOrDefaultAsync(cancellationToken);
        }
        return false;
    }

    internal static IQueryable<Guid?> ParentIdQuery(
        IQueryable<CategoryPersistence> categories,
        Guid categoryId) =>
        categories.AsNoTracking()
            .Where(category => category.Id == categoryId)
            .Select(category => category.ParentCategoryId);

    public async Task<CategoryUsage> GetUsageAsync(
        CategoryId categoryId,
        CancellationToken cancellationToken)
    {
        var childCount = await DirectChildCountQuery(_dbContext.Categories, categoryId)
            .CountAsync(cancellationToken);
        var assignmentCount = await ProductAssignmentCountQuery(_dbContext.ProductCategories, categoryId)
            .CountAsync(cancellationToken);
        return new CategoryUsage(childCount, assignmentCount);
    }

    internal static IQueryable<CategoryPersistence> DirectChildCountQuery(
        IQueryable<CategoryPersistence> categories,
        CategoryId categoryId) =>
        categories.AsNoTracking().Where(category => category.ParentCategoryId == categoryId.Value);

    internal static IQueryable<ProductCategoryPersistence> ProductAssignmentCountQuery(
        IQueryable<ProductCategoryPersistence> assignments,
        CategoryId categoryId) =>
        assignments.AsNoTracking().Where(assignment => assignment.CategoryId == categoryId.Value);

    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(
        int offset,
        int limit,
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly,
        CancellationToken cancellationToken) =>
        await ListQuery(
            _dbContext.Categories, searchTerm, parentCategoryId, rootsOnly, offset, limit)
            .ToListAsync(cancellationToken);

    internal static IQueryable<CategoryListItem> ListQuery(
        IQueryable<CategoryPersistence> categories,
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly,
        int offset = 0,
        int limit = 50)
    {
        if (searchTerm is not null)
            categories = categories.Where(category => category.Name.Contains(searchTerm));
        if (parentCategoryId is not null)
            categories = categories.Where(category =>
                category.ParentCategoryId == parentCategoryId.Value.Value);
        if (rootsOnly)
            categories = categories.Where(category => category.ParentCategoryId == null);

        return categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryListItem(
                category.Id,
                category.Name,
                category.ParentCategoryId,
                category.Children.Count))
            .Skip(offset)
            .Take(limit);
    }
}
