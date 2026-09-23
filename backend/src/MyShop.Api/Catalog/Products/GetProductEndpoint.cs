using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProduct;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProduct.GetProduct;

namespace MyShop.Api.Catalog.Products;

public static class GetProductEndpoint
{
    public static IEndpointRouteBuilder MapGetProduct(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/products/{productId:guid}", ExecuteAsync)
            .WithName("GetProduct");

        return endpoints;
    }

    public static async Task<Results<
        Ok<GetProductResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new GetProductQuery(ProductId.From(productId)),
                cancellationToken);

            if (result.Failure == GetProductFailure.ProductNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product '{productId}' does not exist."
                });

            var snapshot = result.Snapshot
                ?? throw new InvalidOperationException("A successful get result must contain a product snapshot.");

            return TypedResults.Ok(Map(snapshot));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product ID",
                Detail = exception.Message
            });
        }
    }

    private static GetProductResponse Map(
        Application.Catalog.Abstractions.ProductSnapshot snapshot)
    {
        var product = snapshot.Product;
        return new GetProductResponse(
            product.Id.Value,
            product.ProductTypeId.Value,
            product.Name,
            product.CategoryIds.Select(categoryId => categoryId.Value).ToArray(),
            product.AttributeValues.Select(AttributeValueResponse.FromDomain).ToArray(),
            product.Variants.Select(ProductVariantResponse.FromDomain).ToArray(),
            snapshot.ConcurrencyToken.Revision);
    }
}

public sealed record GetProductResponse(
    Guid Id,
    Guid ProductTypeId,
    string Name,
    IReadOnlyList<Guid> CategoryIds,
    IReadOnlyList<AttributeValueResponse> AttributeValues,
    IReadOnlyList<ProductVariantResponse> Variants,
    Guid Revision);

public sealed record ProductVariantResponse(
    Guid Id,
    string Name,
    string? Sku,
    IReadOnlyList<AttributeValueResponse> AttributeValues,
    MoneyResponse? Price,
    IReadOnlyList<PriceRuleResponse> PriceRules)
{
    internal static ProductVariantResponse FromDomain(ProductVariant variant) => new(
        variant.Id.Value,
        variant.Name,
        variant.Sku?.Value,
        variant.AttributeValues.Select(AttributeValueResponse.FromDomain).ToArray(),
        variant.Price is { } price ? new MoneyResponse(price.Amount, price.Currency) : null,
        variant.PriceRules.OrderByDescending(rule => rule.Priority).ThenBy(rule => rule.Id)
            .Select(PriceRuleResponse.FromDomain).ToArray());
}

public sealed record AttributeValueResponse(
    Guid AttributeDefinitionId,
    string DataType,
    object Value)
{
    internal static AttributeValueResponse FromDomain(AttributeValue value) =>
        new(
            value.AttributeDefinitionId.Value,
            value.DataType.ToString(),
            value switch
            {
                TextAttributeValue text => text.Value,
                IntegerAttributeValue integer => integer.Value,
                DecimalAttributeValue decimalValue => decimalValue.Value,
                BooleanAttributeValue boolean => boolean.Value,
                DateAttributeValue date => date.Value,
                ChoiceAttributeValue choice => choice.Value.Value,
                MultiChoiceAttributeValue multiChoice =>
                    multiChoice.Values.Select(choice => choice.Value).ToArray(),
                _ => throw new ArgumentOutOfRangeException(nameof(value), "Attribute value type is not supported.")
            });
}

public sealed record MoneyResponse(decimal Amount, string Currency);
