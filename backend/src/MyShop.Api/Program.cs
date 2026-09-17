using MyShop.Api;
using MyShop.Api.Catalog.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyShop(builder.Configuration);

var app = builder.Build();

app.MapCreateProduct();
app.MapGetProduct();
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
