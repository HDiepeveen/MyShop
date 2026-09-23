using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Api.Catalog.ProductTypes;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.ProductTypes;

internal sealed class ProductTypeManagementScenario : IAsyncDisposable
{
    public ProductTypeManagementScenario()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddSingleton<IProductTypeRepository>(Repository);
        builder.Services.AddSingleton<IProductTypeWriter>(Repository);
        builder.Services.AddSingleton<IProductTypeDeleter>(Repository);
        builder.Services.AddSingleton<IProductTypeUsageRepository>(Repository);
        builder.Services.AddScoped<MyShop.Application.Catalog.CreateProductType.CreateProductType>();
        builder.Services.AddScoped<MyShop.Application.Catalog.GetProductType.GetProductType>();
        builder.Services.AddScoped<MyShop.Application.Catalog.RenameProductType.RenameProductType>();
        builder.Services.AddScoped<MyShop.Application.Catalog.DeleteProductType.DeleteProductType>();
        builder.Services.AddScoped<MyShop.Application.Catalog.AddProductTypeAttribute.AddProductTypeAttribute>();
        builder.Services.AddScoped<MyShop.Application.Catalog.RenameProductTypeAttribute.RenameProductTypeAttribute>();
        builder.Services.AddScoped<MyShop.Application.Catalog.ConfigureProductTypeAttribute.ConfigureProductTypeAttribute>();
        builder.Services.AddScoped<MyShop.Application.Catalog.RemoveProductTypeAttribute.RemoveProductTypeAttribute>();
        builder.Services.AddScoped<MyShop.Application.Catalog.GetProductTypeAttribute.GetProductTypeAttribute>();
        builder.Services.AddScoped<MyShop.Application.Catalog.GetProductTypeUsage.GetProductTypeUsage>();
        var app = builder.Build();
        app.MapCreateProductType();
        app.MapGetProductType();
        app.MapRenameProductType();
        app.MapDeleteProductType();
        app.MapAddProductTypeAttribute();
        app.MapRenameProductTypeAttribute();
        app.MapConfigureProductTypeAttribute();
        app.MapRemoveProductTypeAttribute();
        app.MapGetProductTypeAttribute();
        app.MapGetProductTypeUsage();
        Http = new CatalogManagementHttp(app);
    }

    public Store Repository { get; } = new();
    public CatalogManagementHttp Http { get; }
    public ValueTask DisposeAsync() => Http.DisposeAsync();

    internal sealed class Store : IProductTypeRepository, IProductTypeWriter,
        IProductTypeDeleter, IProductTypeUsageRepository
    {
        public Dictionary<ProductTypeId, ProductType> Items { get; } = [];
        public Dictionary<ProductTypeId, int> ProductCounts { get; } = [];
        public int Reads { get; private set; }
        public int Adds { get; private set; }
        public int Saves { get; private set; }
        public int Deletes { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Reads++;
            return Task.FromResult(Items.GetValueOrDefault(id));
        }

        public Task AddAsync(ProductType productType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(productType.Id, productType);
            Adds++;
            return Task.CompletedTask;
        }

        public Task SaveAsync(ProductType productType, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.Same(Items[productType.Id], productType);
            Saves++;
            return Task.CompletedTask;
        }

        public Task DeleteAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Assert.True(Items.Remove(id));
            Deletes++;
            return Task.CompletedTask;
        }

        public Task<int> CountProductsAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(ProductCounts.GetValueOrDefault(id));
        }
    }
}
