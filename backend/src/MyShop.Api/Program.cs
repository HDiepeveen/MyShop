using MyShop.Api;
using MyShop.Api.Catalog.Categories;
using MyShop.Api.Catalog.Products;
using MyShop.Api.Catalog.ProductTypes;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyShop(builder.Configuration);

var app = builder.Build();

app.MapCreateProduct();
app.MapGetCategory();
app.MapListCategories();
app.MapGetProduct();
app.MapListProductTypes();
app.MapGetProductType();
app.MapRenameProduct();
app.MapAddProductVariant();
app.MapRenameProductVariant();
app.MapSetProductVariantSku();
app.MapRemoveProductVariant();
app.MapAssignProductToCategory();
app.MapRemoveProductFromCategory();
app.MapRemoveProductAttributeValue();
app.MapRemoveVariantAttributeValue();
app.MapSetVariantAttributeValue();
app.MapSetProductAttributeValue();

app.Run();
