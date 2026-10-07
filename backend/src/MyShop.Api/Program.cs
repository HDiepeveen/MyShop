using MyShop.Api.Storefront;
using MyShop.Api.Security;
using MyShop.Api;
using MyShop.Api.Catalog;
using MyShop.Api.Checkout;
using MyShop.Api.Dashboard;



var builder = WebApplication.CreateBuilder(args.Where(arg => arg is not "--create-admin" and not "--reset-admin").ToArray());

builder.Services.AddMyShop(builder.Configuration);

builder.Services.AddAdminSecurity(builder.Environment.IsDevelopment());
builder.Services.AddHostedService<MyShop.Infrastructure.Notifications.EmailDeliveryWorker>();

var app = builder.Build();
if (args.Contains("--create-admin") || args.Contains("--reset-admin"))
{
    await AdminAccountCommand.RunAsync(app.Services, args.Contains("--reset-admin"));
    return;
}
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseAdminSecurity();
app.MapAdminEndpoints();
app.MapCustomerEndpoints();
app.MapCustomerEmailEndpoints();
app.MapCustomerOrderEndpoints();
app.MapCustomerWishlistEndpoints();
app.MapCustomerManagementEndpoints();

app.MapCatalog();
app.MapStorefront();
app.MapPaymentOptions();
app.MapEmailSettings();
app.MapBilling();
app.MapDeliveryMethods();
app.MapOrderManagement();
app.MapDashboard();

app.Run();
