using Microsoft.EntityFrameworkCore;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Mappers;

namespace MyShop.Infrastructure.Persistence.Repositories;

internal sealed class CategoryRepository : ICategoryRepository
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
}
