using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.RenameProduct;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProduct.RenameProduct;

namespace MyShop.Api.Catalog.Products;

public static class RenameProductEndpoint
{
    public static IEndpointRouteBuilder MapRenameProduct(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPatch("/api/products/{productId:guid}/name", ExecuteAsync)
            .WithName("RenameProduct");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        RenameProductRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new RenameProductCommand(ProductId.From(productId), request.Name),
                cancellationToken);

            if (result.Failure == RenameProductFailure.ProductNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product '{productId}' does not exist."
                });

            return TypedResults.NoContent();
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
                Title = "Invalid product rename",
                Detail = exception.Message
            });
        }
    }
}

public sealed record RenameProductRequest(string Name);
