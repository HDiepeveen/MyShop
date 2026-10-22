using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.AddProductVariant;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.AddProductVariant.AddProductVariant;

namespace HDWebFramework.Api.Catalog.Products;

public static class AddProductVariantEndpoint
{
    public static IEndpointRouteBuilder MapAddProductVariant(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/products/{productId:guid}/variants", ExecuteAsync)
            .WithName("AddProductVariant");

        return endpoints;
    }

    public static async Task<Results<
        Created<AddProductVariantResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        AddProductVariantRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new AddProductVariantCommand(ProductId.From(productId), request.Name),
                cancellationToken);

            if (result.Failure == AddProductVariantFailure.ProductNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product not found",
                    Detail = $"Product '{productId}' does not exist."
                });

            var snapshot = result.Snapshot
                ?? throw new InvalidOperationException("A successful add result must contain a product snapshot.");
            var variant = snapshot.Product.Variants.Last();
            var response = new AddProductVariantResponse(
                variant.Id.Value,
                variant.Name,
                snapshot.ConcurrencyToken.Revision);

            return TypedResults.Created(
                $"/api/products/{productId}",
                response);
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
                Title = "Invalid product variant",
                Detail = exception.Message
            });
        }
    }
}

public sealed record AddProductVariantRequest(string Name);

public sealed record AddProductVariantResponse(
    Guid Id,
    string Name,
    Guid Revision);
