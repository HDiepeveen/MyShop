using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.RemoveProductFromCategory;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.RemoveProductFromCategory.RemoveProductFromCategory;

namespace HDWebFramework.Api.Catalog.Products;

public static class RemoveProductFromCategoryEndpoint
{
    public static IEndpointRouteBuilder MapRemoveProductFromCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapDelete(
                "/api/products/{productId:guid}/categories/{categoryId:guid}",
                ExecuteAsync)
            .WithName("RemoveProductFromCategory");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid categoryId,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RemoveProductFromCategoryCommand(
                    ProductId.From(productId),
                    CategoryId.From(categoryId)),
                cancellationToken);

            return result.Failure switch
            {
                RemoveProductFromCategoryFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Remove category failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Product was modified",
                Detail = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product category removal",
                Detail = exception.Message
            });
        }
    }
}
