using HDWebFramework.Api.Security;
using HDWebFramework.Application.Security;

namespace HDWebFramework.Api.Tests.Security;

public sealed class PaymentPermissionTests
{
    [Theory]
    [InlineData("/api/payment-options", "payments")]
    [InlineData("/api/payment-options/mollie", "paymentSettings")]
    [InlineData("/api/payment-options/mollie/check", "paymentSettings")]
    [InlineData("/api/payment-options/mollie-other", "payments")]
    public void ProviderEndpointsRequireTheirOwnPermission(string route, string expected)
        => Assert.Equal(expected, ManagementAuthorization.Permission(route));

    [Fact]
    public void ProviderPermissionIsAssignableButAccountAdministrationIsNot()
    {
        Assert.Equal("Betaalinstellingen", CompanyPermissions.Items["paymentSettings"]);
        Assert.Equal("Website en SEO", CompanyPermissions.Items["seo"]);
        Assert.False(CompanyPermissions.Items.ContainsKey("companyAccess"));
    }
}
