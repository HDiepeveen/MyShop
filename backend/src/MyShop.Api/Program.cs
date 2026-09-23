using MyShop.Api;
using MyShop.Api.Catalog.Categories;
using MyShop.Api.Catalog.Products;
using MyShop.Api.Catalog.ProductTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyShop(builder.Configuration);

var app = builder.Build();

app.MapCreateProduct();
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
app.MapGetProductBySku();
app.MapListProducts();
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

app.Run();
