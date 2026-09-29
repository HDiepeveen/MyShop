namespace MyShop.Domain.Catalog;

public static class ProductAttributeValidation
{
    public static IReadOnlyList<ProductAttributeIssue> Evaluate(Product product, ProductType productType)
    {
        ArgumentNullException.ThrowIfNull(product);
        ArgumentNullException.ThrowIfNull(productType);
        if (product.ProductTypeId != productType.Id)
            throw new ArgumentException("The product type must belong to the product.", nameof(productType));

        var definitions = productType.AttributeDefinitions.ToDictionary(definition => definition.Id);
        var issues = new List<ProductAttributeIssue>();
        EvaluateValues(product.AttributeValues, AttributeScope.Product, null, definitions, issues);
        foreach (var variant in product.Variants)
            EvaluateValues(variant.AttributeValues, AttributeScope.Variant, variant.Id, definitions, issues);
        return issues.OrderBy(issue => issue.VariantId?.Value)
            .ThenBy(issue => issue.AttributeDefinitionId.Value)
            .ThenBy(issue => issue.Code).ToList().AsReadOnly();
    }

    private static void EvaluateValues(
        IReadOnlyCollection<AttributeValue> values,
        AttributeScope scope,
        ProductVariantId? variantId,
        IReadOnlyDictionary<AttributeDefinitionId, AttributeDefinition> definitions,
        List<ProductAttributeIssue> issues)
    {
        foreach (var value in values)
        {
            if (!definitions.TryGetValue(value.AttributeDefinitionId, out var definition))
            {
                issues.Add(new(value.AttributeDefinitionId, variantId, ProductAttributeIssueCode.UnknownDefinition));
                continue;
            }
            if (definition.Scope != scope)
                issues.Add(new(definition.Id, variantId, ProductAttributeIssueCode.WrongScope));
            else if (definition.DataType != value.DataType)
                issues.Add(new(definition.Id, variantId, ProductAttributeIssueCode.WrongDataType));
        }
        var present = values.Select(value => value.AttributeDefinitionId).ToHashSet();
        foreach (var definition in definitions.Values.Where(definition => definition.Scope == scope && definition.IsRequired))
        {
            if (!present.Contains(definition.Id))
                issues.Add(new(definition.Id, variantId, ProductAttributeIssueCode.MissingRequired));
        }
    }
}
