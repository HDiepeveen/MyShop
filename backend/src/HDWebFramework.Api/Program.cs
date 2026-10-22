using HDWebFramework.Api.Storefront;
using HDWebFramework.Api.Security;
using HDWebFramework.Api;
using HDWebFramework.Api.Catalog;
using HDWebFramework.Api.Checkout;
using HDWebFramework.Api.Dashboard;



var builder = WebApplication.CreateBuilder(args.Where(arg => arg is not "--create-admin" and not "--reset-admin" and not "--upgrade-framework").ToArray());

builder.Services.AddHDWebFramework(builder.Configuration);
builder.Services.ConfigureHostingKeys(builder.Configuration, builder.Environment);

builder.Services.AddAdminSecurity(builder.Environment.IsDevelopment());
builder.Services.AddHostedService<HDWebFramework.Infrastructure.Notifications.EmailDeliveryWorker>();

var app = builder.Build();
if (args.Contains("--upgrade-framework"))
{
    await HDWebFramework.Infrastructure.Security.FrameworkSettingsUpgrade.UpgradeAsync(app.Services);
    Console.WriteLine("De opgeslagen geheime instellingen zijn omgezet naar HDWebFramework.");
    return;
}
if (args.Contains("--create-admin") || args.Contains("--reset-admin"))
{
    await AdminAccountCommand.RunAsync(app.Services, args.Contains("--reset-admin"));
    return;
}
if (!app.Environment.IsDevelopment()) { app.UseHsts(); app.UseHttpsRedirection(); }
app.UseStaticFiles();
app.UseAdminSecurity();
app.MapAdminEndpoints();
app.MapCustomerEndpoints();
app.MapCustomerEmailEndpoints();
app.MapCustomerOrderEndpoints();
app.MapCustomerWishlistEndpoints();
app.MapCustomerManagementEndpoints();

app.MapCatalog();
app.MapSeo();
app.MapStorefront();
app.MapPaymentOptions();
app.MapEmailSettings();
app.MapBilling();
app.MapDeliveryMethods();
app.MapOrderManagement();
app.MapDashboard();

app.MapFrontend();
app.Run();
