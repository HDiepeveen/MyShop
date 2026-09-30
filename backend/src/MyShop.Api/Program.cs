using MyShop.Api.Storefront;
using MyShop.Api.Security;
using MyShop.Api;
using MyShop.Api.Catalog;



var builder = WebApplication.CreateBuilder(args.Where(arg => arg is not "--create-admin" and not "--reset-admin").ToArray());

builder.Services.AddMyShop(builder.Configuration);

builder.Services.AddAdminSecurity(builder.Environment.IsDevelopment());

var app = builder.Build();
if (args.Contains("--create-admin") || args.Contains("--reset-admin"))
{
    await AdminAccountCommand.RunAsync(app.Services, args.Contains("--reset-admin"));
    return;
}
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseAdminSecurity();
app.MapAdminEndpoints();

app.MapCatalog();
app.MapStorefront();

app.Run();
