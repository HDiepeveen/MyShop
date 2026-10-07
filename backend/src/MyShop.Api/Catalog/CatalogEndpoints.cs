using MyShop.Api.Catalog.Categories;
using MyShop.Api.Catalog.Products;
using MyShop.Api.Catalog.ProductTypes;

namespace MyShop.Api.Catalog;

public static class CatalogEndpoints
{
    public static void MapCatalog(this IEndpointRouteBuilder app)
    {
        app.MapCreateProduct();
        app.MapUpdateProductPresentation();
        app.MapDeleteProduct();
        app.MapClearProductVariantSku();
        app.MapCreateCategory();
        app.MapCreateProductType();
        app.MapDeleteCategory();
        app.MapDeleteProductType();
        app.MapConfigureProductTypeAttribute();
        app.MapGetCategory();
        app.MapListCategories();
        app.MapGetProduct();
        app.MapGetProductAttributeValidation();
        app.MapGetProductBySku();
        app.MapListProducts();
        app.MapExportProducts();
        app.MapListProductTypes();
        app.MapMoveCategory();
        app.MapRenameProductType();
        app.MapRenameProductTypeAttribute();
        app.MapGetProductType();
        app.MapRenameProduct();
        app.MapRenameCategory();
        app.MapAddProductVariant();
        app.MapAddProductTypeAttribute();
        app.MapRenameProductVariant();
        app.MapSetProductVariantSku();
        app.MapSetProductVariantPrice();
        app.MapSetProductVariantStock();
        app.MapClearProductVariantStock();
        app.MapGetProductTypeUsage();
        app.MapGetCategoryUsage();
        app.MapGetProductTypeAttribute();
        app.MapGetVariantAttributeValue();
        app.MapGetProductAttributeValue();
        app.MapGetProductVariant();
        app.MapListProductVariantPriceRules();
        app.MapGetProductVariantPriceRule();
        app.MapGetProductVariantPrice();
        app.MapUpdateProductVariantPriceRule();
        app.MapRemoveProductVariantPriceRule();
        app.MapAddProductVariantPriceRule();
        app.MapClearProductVariantPrice();
        app.MapRemoveProductVariant();
        app.MapAssignProductToCategory();
        app.MapRemoveProductFromCategory();
        app.MapRemoveProductAttributeValue();
        app.MapRemoveProductTypeAttribute();
        app.MapRemoveVariantAttributeValue();
        app.MapSetVariantAttributeValue();
        app.MapSetProductAttributeValue();
    }
}
