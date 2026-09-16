using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.AssignProductToCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AssignProductToCategory.AssignProductToCategory;

namespace MyShop.Api.Catalog.Products;

public static class AssignProductToCategoryEndpoint
{
    public static IEndpointRouteBuilder MapAssignProductToCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/categories/{categoryId:guid}",
                ExecuteAsync)
            .WithName("AssignProductToCategory");

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
                new AssignProductToCategoryCommand(
                    ProductId.From(productId),
                    CategoryId.From(categoryId)),
                cancellationToken);

            return result.Failure switch
            {
                AssignProductToCategoryFailure.ProductNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Product not found",
                        Detail = $"Product '{productId}' does not exist."
                    }),
                AssignProductToCategoryFailure.CategoryNotFound =>
                    TypedResults.NotFound(new ProblemDetails
                    {
                        Title = "Category not found",
                        Detail = $"Category '{categoryId}' does not exist."
                    }),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Assign category failure '{result.Failure}' is not supported.")
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
                Title = "Invalid product category assignment",
                Detail = exception.Message
            });
        }
    }
}
