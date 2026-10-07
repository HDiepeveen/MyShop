using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using MyShop.Infrastructure.Persistence.Models;

namespace MyShop.Infrastructure.Persistence;

public sealed class MyShopDbContext(DbContextOptions<MyShopDbContext> options) : IdentityDbContext<IdentityUser>(options)
{
    internal DbSet<ProductTypePersistence> ProductTypes => Set<ProductTypePersistence>();
    internal DbSet<AttributeDefinitionPersistence> AttributeDefinitions => Set<AttributeDefinitionPersistence>();
    internal DbSet<CategoryPersistence> Categories => Set<CategoryPersistence>();
    internal DbSet<ProductPersistence> Products => Set<ProductPersistence>();
    internal DbSet<ProductVariantPersistence> ProductVariants => Set<ProductVariantPersistence>();
    internal DbSet<PriceRulePersistence> PriceRules => Set<PriceRulePersistence>();
    internal DbSet<ProductCategoryPersistence> ProductCategories => Set<ProductCategoryPersistence>();
    internal DbSet<ProductAttributeValuePersistence> ProductAttributeValues => Set<ProductAttributeValuePersistence>();
    internal DbSet<ProductVariantAttributeValuePersistence> ProductVariantAttributeValues => Set<ProductVariantAttributeValuePersistence>();
    internal DbSet<ProductAttributeMultiChoiceValuePersistence> ProductAttributeMultiChoiceValues => Set<ProductAttributeMultiChoiceValuePersistence>();
    internal DbSet<ProductVariantAttributeMultiChoiceValuePersistence> ProductVariantAttributeMultiChoiceValues => Set<ProductVariantAttributeMultiChoiceValuePersistence>();
    internal DbSet<PaymentOptionsPersistence> PaymentOptions => Set<PaymentOptionsPersistence>();
    internal DbSet<DeliveryMethodPersistence> DeliveryMethods => Set<DeliveryMethodPersistence>();
    internal DbSet<OrderPersistence> Orders => Set<OrderPersistence>();
    internal DbSet<OrderLinePersistence> OrderLines => Set<OrderLinePersistence>();
    internal DbSet<OrderTotalPersistence> OrderTotals => Set<OrderTotalPersistence>();
    internal DbSet<OnlinePaymentStartPersistence> OnlinePaymentStarts => Set<OnlinePaymentStartPersistence>();
    internal DbSet<OnlinePaymentStartLinePersistence> OnlinePaymentStartLines => Set<OnlinePaymentStartLinePersistence>();
    internal DbSet<OnlinePaymentStartTotalPersistence> OnlinePaymentStartTotals => Set<OnlinePaymentStartTotalPersistence>();
    internal DbSet<CustomerProfilePersistence> CustomerProfiles => Set<CustomerProfilePersistence>();
    internal DbSet<WishlistItemPersistence> WishlistItems => Set<WishlistItemPersistence>();
    internal DbSet<EmailMessagePersistence> EmailMessages => Set<EmailMessagePersistence>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MyShopDbContext).Assembly);
    }
}
