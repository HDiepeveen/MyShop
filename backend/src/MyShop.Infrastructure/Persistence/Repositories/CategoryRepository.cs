using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository : ICategoryRepository, ICategoryListRepository, ICategoryWriter
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

    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly,
        CancellationToken cancellationToken) =>
        await ListQuery(
            _dbContext.Categories, searchTerm, parentCategoryId, rootsOnly)
            .ToListAsync(cancellationToken);

    internal static IQueryable<CategoryListItem> ListQuery(
        IQueryable<CategoryPersistence> categories,
        string? searchTerm,
        CategoryId? parentCategoryId,
        bool rootsOnly)
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
                category.Children.Count));
    }
}
