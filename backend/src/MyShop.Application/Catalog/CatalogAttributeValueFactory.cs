using MyShop.Domain.Catalog;

namespace MyShop.Application.Catalog;

internal static class CatalogAttributeValueFactory
{
    public static AttributeValue Create(
        AttributeDefinitionId attributeDefinitionId,
        CatalogAttributeValueInput input) => input switch
    {
        TextAttributeValueInput text => TextAttributeValue.Create(attributeDefinitionId, text.Value),
        IntegerAttributeValueInput integer => IntegerAttributeValue.Create(attributeDefinitionId, integer.Value),
        DecimalAttributeValueInput decimalValue => DecimalAttributeValue.Create(attributeDefinitionId, decimalValue.Value),
        BooleanAttributeValueInput boolean => BooleanAttributeValue.Create(attributeDefinitionId, boolean.Value),
        DateAttributeValueInput date => DateAttributeValue.Create(attributeDefinitionId, date.Value),
        ChoiceAttributeValueInput choice => ChoiceAttributeValue.Create(
            attributeDefinitionId,
            ChoiceValue.Create(choice.Value)),
        MultiChoiceAttributeValueInput multiChoice => MultiChoiceAttributeValue.Create(
            attributeDefinitionId,
            multiChoice.Values.Select(ChoiceValue.Create)),
        _ => throw new ArgumentOutOfRangeException(nameof(input), "Attribute value input type is not supported.")
    };
}
