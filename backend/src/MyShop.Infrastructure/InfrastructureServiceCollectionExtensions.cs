using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Checkout.Abstractions;
using MyShop.Application.Customers.Abstractions;
using MyShop.Application.Dashboard.Abstractions;
using MyShop.Infrastructure.Persistence;
using MyShop.Infrastructure.Persistence.Repositories;
using MyShop.Infrastructure.Payments;

namespace MyShop.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddMyShopInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<MyShopDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IProductRepository>(provider =>
            new ProductRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductDeleter>(provider =>
            new ProductRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductListRepository>(provider =>
            new ProductListRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductTypeRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductTypeListRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductTypeWriter>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductTypeUsageRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductTypeDeleter>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICategoryRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICategoryListRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICategoryWriter>(provider =>
            new CategoryRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICategoryHierarchyRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICategoryUsageRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IProductSkuLookup>(provider =>
            new ProductSkuLookup(provider.GetRequiredService<MyShopDbContext>()));

        services.AddScoped<IStorefrontCatalog>(provider => new StorefrontCatalog(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IPaymentOptionsRepository>(provider =>
            new PaymentOptionsRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IDeliveryMethodRepository>(provider =>
            new DeliveryMethodRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IOnlinePaymentAvailability>(_ => new OnlinePaymentAvailability());
        services.AddScoped<IOrderRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IOrderReadRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IOrderStatusRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICustomerOrderReadRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICustomerOrderCancellationRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IManagedCustomerRepository>(provider =>
            new ManagedCustomerRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<ICustomerProfileRepository>(provider =>
            new CustomerProfileRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IWishlistRepository>(provider =>
            new WishlistRepository(provider.GetRequiredService<MyShopDbContext>()));
        services.AddScoped<IDashboardReadRepository>(provider =>
            new DashboardReadRepository(provider.GetRequiredService<MyShopDbContext>()));
        return services;
    }
}
