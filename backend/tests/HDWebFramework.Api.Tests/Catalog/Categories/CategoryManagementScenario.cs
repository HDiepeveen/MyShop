using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Api.Catalog.Categories;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Api.Tests.Catalog.Categories;

internal sealed class CategoryManagementScenario : IAsyncDisposable
{
    public CategoryManagementScenario()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<ICategoryRepository>(Repository);
        builder.Services.AddSingleton<ICategoryWriter>(Repository);
        builder.Services.AddSingleton<ICategoryHierarchyRepository>(Repository);
        builder.Services.AddSingleton<ICategoryUsageRepository>(Repository);
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.CreateCategory.CreateCategory>();
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.GetCategory.GetCategory>();
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.RenameCategory.RenameCategory>();
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.MoveCategory.MoveCategory>();
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.DeleteCategory.DeleteCategory>();
        builder.Services.AddScoped<HDWebFramework.Application.Catalog.GetCategoryUsage.GetCategoryUsage>();
        var app = builder.Build();
        app.MapCreateCategory();
        app.MapGetCategory();
        app.MapRenameCategory();
        app.MapMoveCategory();
        app.MapDeleteCategory();
        app.MapGetCategoryUsage();
        Http = new CatalogManagementHttp(app);
    }

    public Store Repository { get; } = new();
    public CatalogManagementHttp Http { get; }
    public ValueTask DisposeAsync() => Http.DisposeAsync();

    internal sealed class Store : ICategoryRepository, ICategoryWriter,
        ICategoryHierarchyRepository, ICategoryUsageRepository
    {
        public Dictionary<CategoryId, Category> Items { get; } = [];
        public Dictionary<CategoryId, int> Assignments { get; } = [];
        public int Reads { get; private set; }
        public int Adds { get; private set; }
        public int Saves { get; private set; }
        public int Deletes { get; private set; }
        public CancellationToken LastToken { get; private set; }

        private void Observe(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            LastToken = token;
        }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            Reads++;
            return Task.FromResult(Items.GetValueOrDefault(id));
        }

        public Task AddAsync(Category category, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            Items.Add(category.Id, category);
            Adds++;
            return Task.CompletedTask;
        }

        public Task SaveAsync(Category category, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            Assert.Same(Items[category.Id], category);
            Saves++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(CategoryId id, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            Assert.True(Items.Remove(id));
            Deletes++;
            return Task.CompletedTask;
        }

        public Task<CategoryUsage> GetUsageAsync(CategoryId id, CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            return Task.FromResult(new CategoryUsage(
                Items.Values.Count(item => item.ParentCategoryId == id), Assignments.GetValueOrDefault(id)));
        }

        public Task<bool> IsDescendantOfAsync(CategoryId candidateId, CategoryId ancestorId,
            CancellationToken cancellationToken)
        {
            Observe(cancellationToken);
            var visited = new HashSet<CategoryId>();
            CategoryId? current = candidateId;
            while (current is not null && visited.Add(current.Value))
            {
                if (current == ancestorId) return Task.FromResult(true);
                current = Items.GetValueOrDefault(current.Value)?.ParentCategoryId;
            }
            return Task.FromResult(false);
        }
    }
}
