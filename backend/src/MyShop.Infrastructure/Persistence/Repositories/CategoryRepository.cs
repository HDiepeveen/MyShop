using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository : ICategoryRepository, ICategoryListRepository
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

    public async Task<IReadOnlyList<CategoryListItem>> ListAsync(CancellationToken cancellationToken) =>
        await ListQuery(_dbContext.Categories).ToListAsync(cancellationToken);

    internal static IQueryable<CategoryListItem> ListQuery(
        IQueryable<CategoryPersistence> categories) =>
        categories
            .AsNoTracking()
            .OrderBy(category => category.Name)
            .ThenBy(category => category.Id)
            .Select(category => new CategoryListItem(
                category.Id,
                category.Name,
                category.ParentCategoryId));
}
