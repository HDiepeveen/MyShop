using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Checkout.Abstractions;
using HDWebFramework.Application.Customers.Abstractions;
using HDWebFramework.Application.Dashboard.Abstractions;
using HDWebFramework.Infrastructure.Persistence;
using HDWebFramework.Infrastructure.Persistence.Repositories;
using HDWebFramework.Infrastructure.Payments;

namespace HDWebFramework.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddHDWebFrameworkInfrastructure(
        this IServiceCollection services,
        string connectionString) =>
        services.AddHDWebFrameworkInfrastructure(connectionString, new ConfigurationBuilder().Build());

    public static IServiceCollection AddHDWebFrameworkInfrastructure(
        this IServiceCollection services,
        string connectionString,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddScoped<HDWebFramework.Application.Catalog.CategoryNavigation.ICategoryNavigationStore, CategoryNavigationStore>();
        services.AddScoped<HDWebFramework.Application.Catalog.Seo.ICatalogSeoStore, CatalogSeoStore>();
        services.AddScoped<HDWebFramework.Application.Security.ICompanyAccess, HDWebFramework.Infrastructure.Security.CompanyAccess>();
        services.AddDbContext<HDWebFrameworkDbContext>(options =>
            options.UseSqlServer(connectionString));
        services.AddScoped<IProductImages>(provider => new ProductImagesRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<HDWebFramework.Application.Notifications.IEmailQueue, HDWebFramework.Infrastructure.Notifications.EmailQueue>();
        services.AddScoped<HDWebFramework.Infrastructure.Notifications.EmailSettingsRepository>();
        services.AddScoped<HDWebFramework.Application.Notifications.IEmailSettingsRepository>(provider => provider.GetRequiredService<HDWebFramework.Infrastructure.Notifications.EmailSettingsRepository>());
        services.AddScoped<HDWebFramework.Application.Billing.IBillingSettingsRepository, BillingRepository>();
        services.AddScoped<HDWebFramework.Application.Billing.IInvoiceRepository, BillingRepository>();
        services.AddScoped<IVatRateAvailability>(provider => new BillingRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductRepository>(provider =>
            new ProductRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductDeleter>(provider =>
            new ProductRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductListRepository>(provider =>
            new ProductListRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductTypeRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductTypeListRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductTypeWriter>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductTypeUsageRepository>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductTypeDeleter>(provider =>
            new ProductTypeRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICategoryRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICategoryListRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICategoryWriter>(provider =>
            new CategoryRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICategoryHierarchyRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICategoryUsageRepository>(provider =>
            new CategoryRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IProductSkuLookup>(provider =>
            new ProductSkuLookup(provider.GetRequiredService<HDWebFrameworkDbContext>()));

        services.AddScoped<IStorefrontCatalog>(provider => new StorefrontCatalog(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IPaymentOptionsRepository>(provider =>
            new PaymentOptionsRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IDeliveryMethodRepository>(provider =>
            new DeliveryMethodRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<MollieSettingsStore>();
        services.AddScoped<HDWebFramework.Application.Checkout.MollieSettings.IMollieSettingsStore>(p => p.GetRequiredService<MollieSettingsStore>());
        services.AddScoped<HDWebFramework.Application.Checkout.MollieSettings.ManageMollieSettings>();
        services.AddScoped<IOnlinePaymentAvailability>(p => new OnlinePaymentAvailability(configuration, p.GetRequiredService<MollieSettingsStore>()));
        services.AddHttpClient("Mollie", client => client.Timeout = TimeSpan.FromSeconds(20))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
        services.AddScoped<OnlinePaymentProvider>(provider => new(provider.GetRequiredService<IHttpClientFactory>(), configuration, provider.GetRequiredService<MollieSettingsStore>()));
        services.AddScoped<IOnlinePaymentProvider>(provider => provider.GetRequiredService<OnlinePaymentProvider>());
        services.AddScoped<IOnlinePaymentStatusReader>(provider => provider.GetRequiredService<OnlinePaymentProvider>());
        services.AddScoped<IOnlinePaymentStartRepository, OnlinePaymentStartRepository>();
        services.AddScoped<IOrderRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IOrderReadRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IOrderStatusRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<HDWebFramework.Application.Checkout.CorrectOrderStatus.IOrderCorrectionRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICustomerOrderReadRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICustomerOrderCancellationRepository>(provider =>
            new OrderRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IManagedCustomerRepository>(provider =>
            new ManagedCustomerRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<ICustomerProfileRepository>(provider =>
            new CustomerProfileRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IWishlistRepository>(provider =>
            new WishlistRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        services.AddScoped<IDashboardReadRepository>(provider =>
            new DashboardReadRepository(provider.GetRequiredService<HDWebFrameworkDbContext>()));
        return services;
    }
}
