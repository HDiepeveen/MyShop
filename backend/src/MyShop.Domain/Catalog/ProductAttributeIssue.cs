namespace MyShop.Domain.Catalog;

public enum ProductAttributeIssueCode
{
    MissingRequired,
    WrongScope,
    WrongDataType,
    UnknownDefinition
}

public sealed record ProductAttributeIssue(
    AttributeDefinitionId AttributeDefinitionId,
    ProductVariantId? VariantId,
    ProductAttributeIssueCode Code);
