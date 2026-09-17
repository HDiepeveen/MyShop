using Microsoft.Extensions.DependencyInjection;
using MyShop.Application.Catalog.AddProductVariant;
using MyShop.Application.Catalog.AddProductTypeAttribute;
using MyShop.Application.Catalog.AssignProductToCategory;
using MyShop.Application.Catalog.CreateProduct;
using MyShop.Application.Catalog.CreateCategory;
using MyShop.Application.Catalog.CreateProductType;
using MyShop.Application.Catalog.DeleteCategory;
using MyShop.Application.Catalog.DeleteProductType;
using MyShop.Application.Catalog.ConfigureProductTypeAttribute;
using MyShop.Application.Catalog.GetCategory;
using MyShop.Application.Catalog.GetProduct;
using MyShop.Application.Catalog.GetProductBySku;
using MyShop.Application.Catalog.GetProductType;
using MyShop.Application.Catalog.ListCategories;
using MyShop.Application.Catalog.ListProducts;
using MyShop.Application.Catalog.ListProductTypes;
using MyShop.Application.Catalog.MoveCategory;
using MyShop.Application.Catalog.RemoveProductAttributeValue;
using MyShop.Application.Catalog.RemoveProductTypeAttribute;
using MyShop.Application.Catalog.RemoveProductFromCategory;
using MyShop.Application.Catalog.RemoveProductVariant;
using MyShop.Application.Catalog.RemoveVariantAttributeValue;
using MyShop.Application.Catalog.RenameProduct;
using MyShop.Application.Catalog.RenameCategory;
using MyShop.Application.Catalog.RenameProductType;
using MyShop.Application.Catalog.RenameProductTypeAttribute;
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
        services.AddScoped<AddProductTypeAttribute>();
        services.AddScoped<AssignProductToCategory>();
        services.AddScoped<CreateProduct>();
        services.AddScoped<CreateCategory>();
        services.AddScoped<CreateProductType>();
        services.AddScoped<DeleteCategory>();
        services.AddScoped<DeleteProductType>();
        services.AddScoped<ConfigureProductTypeAttribute>();
        services.AddScoped<GetCategory>();
        services.AddScoped<GetProduct>();
        services.AddScoped<GetProductBySku>();
        services.AddScoped<GetProductType>();
        services.AddScoped<ListCategories>();
        services.AddScoped<ListProducts>();
        services.AddScoped<ListProductTypes>();
        services.AddScoped<MoveCategory>();
        services.AddScoped<RemoveProductAttributeValue>();
        services.AddScoped<RemoveProductTypeAttribute>();
        services.AddScoped<RemoveProductFromCategory>();
        services.AddScoped<RemoveProductVariant>();
        services.AddScoped<RemoveVariantAttributeValue>();
        services.AddScoped<RenameProduct>();
        services.AddScoped<RenameCategory>();
        services.AddScoped<RenameProductType>();
        services.AddScoped<RenameProductTypeAttribute>();
        services.AddScoped<RenameProductVariant>();
        services.AddScoped<SetProductAttributeValue>();
        services.AddScoped<SetProductVariantSku>();
        services.AddScoped<SetVariantAttributeValue>();

        return services;
    }
}
