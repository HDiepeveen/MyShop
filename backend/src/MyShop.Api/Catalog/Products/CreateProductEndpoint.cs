using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.CreateProduct;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateProduct.CreateProduct;

namespace MyShop.Api.Catalog.Products;

public static class CreateProductEndpoint
{
    public static IEndpointRouteBuilder MapCreateProduct(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPost("/api/products", ExecuteAsync)
            .WithName("CreateProduct");

        return endpoints;
    }

    public static async Task<Results<
        Created<CreateProductResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        CreateProductRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var command = new CreateProductCommand(
                ProductTypeId.From(request.ProductTypeId),
                request.Name,
                request.InitialVariantName);
            var result = await useCase.ExecuteAsync(command, cancellationToken);

            if (result.Failure == CreateProductFailure.ProductTypeNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product type not found",
                    Detail = $"Product type '{request.ProductTypeId}' does not exist."
                });

            var snapshot = result.Snapshot
                ?? throw new InvalidOperationException("A successful create result must contain a product snapshot.");
            var product = snapshot.Product;
            var variant = product.Variants.Single();
            var response = new CreateProductResponse(
                product.Id.Value,
                product.ProductTypeId.Value,
                product.Name,
                variant.Id.Value,
                variant.Name,
                snapshot.ConcurrencyToken.Revision);

            return TypedResults.Created($"/api/products/{product.Id.Value}", response);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product",
                Detail = exception.Message
            });
        }
    }
}

public sealed record CreateProductRequest(
    Guid ProductTypeId,
    string Name,
    string InitialVariantName);

public sealed record CreateProductResponse(
    Guid Id,
    Guid ProductTypeId,
    string Name,
    Guid InitialVariantId,
    string InitialVariantName,
    Guid Revision);
