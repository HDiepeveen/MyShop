using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetProductAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetProductAttributeValue.SetProductAttributeValue;

namespace MyShop.Api.Catalog.Products;

public static class SetProductAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapSetProductAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("SetProductAttributeValue");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid attributeDefinitionId,
        AttributeValueRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetProductAttributeValueCommand(
                    ProductId.From(productId),
                    AttributeDefinitionId.From(attributeDefinitionId),
                    AttributeValueRequestMapper.Map(request)),
                cancellationToken);

            return result.Failure switch
            {
                SetProductAttributeValueFailure.ProductNotFound => NotFound(
                    "Product not found", $"Product '{productId}' does not exist."),
                SetProductAttributeValueFailure.ProductTypeNotFound => NotFound(
                    "Product type not found", "The product type does not exist."),
                SetProductAttributeValueFailure.AttributeDefinitionNotFound => NotFound(
                    "Attribute definition not found",
                    $"Attribute definition '{attributeDefinitionId}' does not exist on the product type."),
                SetProductAttributeValueFailure.WrongAttributeScope => Conflict(
                    "Wrong attribute scope", "The attribute definition is not scoped to products."),
                SetProductAttributeValueFailure.WrongAttributeDataType => Conflict(
                    "Wrong attribute data type",
                    $"The attribute definition does not accept '{request.DataType}' values."),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set product attribute failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return Conflict("Product was modified", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product attribute value",
                Detail = exception.Message
            });
        }
    }

    private static NotFound<ProblemDetails> NotFound(string title, string detail) =>
        TypedResults.NotFound(new ProblemDetails { Title = title, Detail = detail });

    private static Conflict<ProblemDetails> Conflict(string title, string detail) =>
        TypedResults.Conflict(new ProblemDetails { Title = title, Detail = detail });
}
