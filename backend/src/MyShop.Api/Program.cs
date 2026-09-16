using MyShop.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMyShop(builder.Configuration);

var app = builder.Build();

app.Run();
