using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.AddProductVariant;
using MyShop.Application.Catalog.AssignProductToCategory;
using MyShop.Application.Catalog.CreateProduct;
using MyShop.Application.Catalog.GetCategory;
using MyShop.Application.Catalog.GetProduct;
using MyShop.Application.Catalog.GetProductType;
using MyShop.Application.Catalog.RemoveProductAttributeValue;
using MyShop.Application.Catalog.RemoveProductFromCategory;
using MyShop.Application.Catalog.RemoveProductVariant;
using MyShop.Application.Catalog.RemoveVariantAttributeValue;
using MyShop.Application.Catalog.RenameProduct;
using MyShop.Application.Catalog.RenameProductVariant;
using MyShop.Application.Catalog.SetProductAttributeValue;
using MyShop.Application.Catalog.SetProductVariantSku;
using MyShop.Application.Catalog.SetVariantAttributeValue;

namespace MyShop.Application;

public static class ApplicationServiceCollectionExtensions
{
    public static IServiceCollection AddMyShopApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<AddProductVariant>();
        services.AddScoped<AssignProductToCategory>();
        services.AddScoped<CreateProduct>();
        services.AddScoped<GetCategory>();
        services.AddScoped<GetProduct>();
        services.AddScoped<GetProductType>();
        services.AddScoped<RemoveProductAttributeValue>();
        services.AddScoped<RemoveProductFromCategory>();
        services.AddScoped<RemoveProductVariant>();
        services.AddScoped<RemoveVariantAttributeValue>();
        services.AddScoped<RenameProduct>();
        services.AddScoped<RenameProductVariant>();
        services.AddScoped<SetProductAttributeValue>();
        services.AddScoped<SetProductVariantSku>();
        services.AddScoped<SetVariantAttributeValue>();

        return services;
    }
}
