using MyShop.Api;
using MyShop.Api.Catalog.Products;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyShop(builder.Configuration);

var app = builder.Build();

app.MapCreateProduct();
app.MapGetProduct();

app.Run();
