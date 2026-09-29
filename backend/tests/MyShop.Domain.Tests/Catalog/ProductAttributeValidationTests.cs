using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductAttributeValidationTests
{
    [Fact]
    public void ReportsMissingRequiredProductAttributeButNotOptionalAttribute()
    {
        var type = ProductType.Create("Type");
        var required = Add(type, AttributeScope.Product, true);
        Add(type, AttributeScope.Product, false);
        var product = Product.Create("Product", type.Id, "First");
        var issue = Assert.Single(ProductAttributeValidation.Evaluate(product, type));
        Assert.Equal(new ProductAttributeIssue(required.Id, null, ProductAttributeIssueCode.MissingRequired), issue);
        Assert.Empty(product.AttributeValues);
    }

    [Fact]
    public void PresentFalseBooleanSatisfiesRequiredAttribute()
    {
        var type = ProductType.Create("Type");
        var definition = Add(type, AttributeScope.Product, true, AttributeDataType.Boolean);
        var product = Product.Create("Product", type.Id, "First");
        product.SetAttributeValue(BooleanAttributeValue.Create(definition.Id, false));
        Assert.Empty(ProductAttributeValidation.Evaluate(product, type));
    }

    [Fact]
    public void NoDefinitionsHasNoIssuesAndInvalidInputsAreRejected()
    {
        var type = ProductType.Create("Type");
        var product = Product.Create("Product", type.Id, "First");
        Assert.Empty(ProductAttributeValidation.Evaluate(product, type));
        Assert.Throws<ArgumentNullException>(() => ProductAttributeValidation.Evaluate(null!, type));
        Assert.Throws<ArgumentNullException>(() => ProductAttributeValidation.Evaluate(product, null!));
        Assert.Throws<ArgumentException>(() => ProductAttributeValidation.Evaluate(product, ProductType.Create("Other")));
    }


    [Fact]
    public void RequiredVariantAttributeIsCheckedForEveryVariantIndependently()
    {
        var type = ProductType.Create("Type");
        var definition = Add(type, AttributeScope.Variant, true);
        var product = Product.Create("Product", type.Id, "First");
        var first = product.Variants.Single();
        var second = product.AddVariant("Second");
        product.SetVariantAttributeValue(first.Id, TextAttributeValue.Create(definition.Id, "Value"));
        var issue = Assert.Single(ProductAttributeValidation.Evaluate(product, type));
        Assert.Equal(new ProductAttributeIssue(definition.Id, second.Id, ProductAttributeIssueCode.MissingRequired), issue);
        product.SetVariantAttributeValue(second.Id, TextAttributeValue.Create(definition.Id, "Value"));
        Assert.Empty(ProductAttributeValidation.Evaluate(product, type));
    }

    [Fact]
    public void ProductValueDoesNotSatisfyVariantRequirement()
    {
        var type = ProductType.Create("Type");
        var definition = Add(type, AttributeScope.Variant, true);
        var product = Product.Create("Product", type.Id, "First");
        product.SetAttributeValue(TextAttributeValue.Create(definition.Id, "Value"));
        Assert.Contains(ProductAttributeValidation.Evaluate(product, type),
            issue => issue.Code == ProductAttributeIssueCode.MissingRequired && issue.VariantId == product.Variants.Single().Id);
    }


    [Theory]
    [InlineData(AttributeScope.Product)]
    [InlineData(AttributeScope.Variant)]
    public void WrongDataTypeIsReportedOnceInsteadOfAsMissing(AttributeScope scope)
    {
        var type = ProductType.Create("Type");
        var definition = Add(type, scope, true, AttributeDataType.Integer);
        var product = Product.Create("Product", type.Id, "First");
        var value = TextAttributeValue.Create(definition.Id, "not an integer");
        if (scope == AttributeScope.Product) product.SetAttributeValue(value);
        else product.SetVariantAttributeValue(product.Variants.Single().Id, value);
        var issue = Assert.Single(ProductAttributeValidation.Evaluate(product, type));
        Assert.Equal(ProductAttributeIssueCode.WrongDataType, issue.Code);
        Assert.Equal(definition.Id, issue.AttributeDefinitionId);
        Assert.Equal(scope == AttributeScope.Product ? (ProductVariantId?)null : product.Variants.Single().Id, issue.VariantId);
    }

    [Theory]
    [InlineData(AttributeScope.Product)]
    [InlineData(AttributeScope.Variant)]
    public void WrongScopeTakesPrecedenceOverDataTypeMismatch(AttributeScope actualScope)
    {
        var type = ProductType.Create("Type");
        var expectedScope = actualScope == AttributeScope.Product ? AttributeScope.Variant : AttributeScope.Product;
        var definition = Add(type, expectedScope, false, AttributeDataType.Integer);
        var product = Product.Create("Product", type.Id, "First");
        var value = TextAttributeValue.Create(definition.Id, "value");
        if (actualScope == AttributeScope.Product) product.SetAttributeValue(value);
        else product.SetVariantAttributeValue(product.Variants.Single().Id, value);
        Assert.Equal(ProductAttributeIssueCode.WrongScope, Assert.Single(ProductAttributeValidation.Evaluate(product, type)).Code);
    }


    [Theory]
    [InlineData(AttributeScope.Product)]
    [InlineData(AttributeScope.Variant)]
    public void RemovedDefinitionIsReportedWithoutDeletingStoredValue(AttributeScope scope)
    {
        var type = ProductType.Create("Type");
        var definition = Add(type, scope, false);
        var product = Product.Create("Product", type.Id, "First");
        var variant = product.Variants.Single();
        var value = TextAttributeValue.Create(definition.Id, "retained");
        if (scope == AttributeScope.Product) product.SetAttributeValue(value);
        else product.SetVariantAttributeValue(variant.Id, value);
        type.RemoveAttribute(definition.Id);
        var issue = Assert.Single(ProductAttributeValidation.Evaluate(product, type));
        Assert.Equal(ProductAttributeIssueCode.UnknownDefinition, issue.Code);
        Assert.Equal(definition.Id, issue.AttributeDefinitionId);
        Assert.Same(value, Assert.Single(scope == AttributeScope.Product ? product.AttributeValues : variant.AttributeValues));
    }

    [Fact]
    public void ResultsAreStableSnapshotsAndCannotBeMutated()
    {
        var type = ProductType.Create("Type");
        Add(type, AttributeScope.Variant, true);
        Add(type, AttributeScope.Product, true);
        Add(type, AttributeScope.Product, true);
        var product = Product.Create("Product", type.Id, "First");
        product.AddVariant("Second");
        var issues = ProductAttributeValidation.Evaluate(product, type);
        Assert.Equal(issues.OrderBy(issue => issue.VariantId?.Value).ThenBy(issue => issue.AttributeDefinitionId.Value), issues);
        Assert.Equal(issues, ProductAttributeValidation.Evaluate(product, type));
        Assert.Throws<NotSupportedException>(() => ((IList<ProductAttributeIssue>)issues).Clear());
        foreach (var definition in type.AttributeDefinitions)
            type.SetAttributeRequired(definition.Id, false);
        Assert.Empty(ProductAttributeValidation.Evaluate(product, type));
        Assert.Equal(4, issues.Count);
    }

    private static AttributeDefinition Add(ProductType type, AttributeScope scope, bool required,
        AttributeDataType dataType = AttributeDataType.Text) =>
        type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("a" + Guid.NewGuid().ToString("N")),
            "Attribute", dataType, required, false, scope);
}
