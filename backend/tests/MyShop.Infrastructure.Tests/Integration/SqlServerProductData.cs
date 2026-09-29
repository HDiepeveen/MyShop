using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using MyShop.Infrastructure.Persistence.Repositories;

namespace MyShop.Infrastructure.Tests.Integration;

internal sealed class SqlServerProductData
{
    private SqlServerProductData()
    {
        Type = ProductType.Create("Type " + Guid.NewGuid().ToString("N"));
        Category = Category.CreateRoot("Category " + Guid.NewGuid().ToString("N"));
        Product = Product.Create("Product " + Guid.NewGuid().ToString("N"), Type.Id, "First");
        Variant = Product.Variants.Single();
        Other = Product.AddVariant("Other");
        Product.AssignToCategory(Category.Id);
        Product.SetVariantSku(Variant.Id, Sku.Create("SKU-" + Guid.NewGuid().ToString("N")));
        Product.SetVariantPrice(Variant.Id, Money.Create(19.95m, "EUR"));
        Rule = PriceRule.Create("Discount", PriceAdjustmentType.PercentageDiscount, 10, 1,
            new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.FromHours(2)));
        Product.AddVariantPriceRule(Variant.Id, Rule);
        foreach (var scope in Enum.GetValues<AttributeScope>())
        foreach (var dataType in Enum.GetValues<AttributeDataType>())
        {
            var id = AttributeDefinitionId.New();
            Definitions.Add((scope, dataType), id);
            Type.AddAttribute(id, AttributeCode.Create($"{scope}_{dataType}".ToLowerInvariant()),
                dataType.ToString(), dataType, false, true, scope);
            var value = CreateValue(id, dataType);
            if (scope == AttributeScope.Product) Product.SetAttributeValue(value);
            else Product.SetVariantAttributeValue(Variant.Id, value);
        }
    }

    public ProductType Type { get; }
    public Category Category { get; }
    public Product Product { get; }
    public ProductVariant Variant { get; }
    public ProductVariant Other { get; }
    public PriceRule Rule { get; }
    public ProductConcurrencyToken Token { get; private set; } = null!;
    public Dictionary<(AttributeScope, AttributeDataType), AttributeDefinitionId> Definitions { get; } = [];

    private static AttributeValue CreateValue(AttributeDefinitionId id, AttributeDataType type) => type switch
    {
        AttributeDataType.Text => TextAttributeValue.Create(id, " Text with spaces "),
        AttributeDataType.Integer => IntegerAttributeValue.Create(id, long.MaxValue),
        AttributeDataType.Decimal => DecimalAttributeValue.Create(id, 1.2345678901234567890123456789m),
        AttributeDataType.Boolean => BooleanAttributeValue.Create(id, false),
        AttributeDataType.Date => DateAttributeValue.Create(id, new DateOnly(2026, 2, 28)),
        AttributeDataType.Choice => ChoiceAttributeValue.Create(id, ChoiceValue.Create("Blue")),
        AttributeDataType.MultiChoice => MultiChoiceAttributeValue.Create(id,
            [ChoiceValue.Create("Blue"), ChoiceValue.Create("Red")]),
        _ => throw new ArgumentOutOfRangeException(nameof(type))
    };

    public static async Task<SqlServerProductData> Seed(SqlServerDatabase database)
    {
        var data = new SqlServerProductData();
        await using var context = database.CreateContext();
        await new ProductTypeRepository(context).AddAsync(data.Type, default);
        await new CategoryRepository(context).AddAsync(data.Category, default);
        data.Token = await new ProductRepository(context).AddAsync(data.Product, default);
        return data;
    }
}
