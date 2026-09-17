using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductBySku;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductBySku.GetProductBySku;

namespace MyShop.Api.Catalog.Products;

public static class GetProductBySkuEndpoint
{
    public static IEndpointRouteBuilder MapGetProductBySku(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/api/products/by-sku/{sku}", ExecuteAsync)
            .WithName("GetProductBySku");

        return endpoints;
    }

    public static async Task<Results<
        Ok<ProductSkuOwnerResponse>,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>>> ExecuteAsync(
        string sku,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var owner = await useCase.ExecuteAsync(
                new GetProductBySkuQuery(Sku.Create(sku)),
                cancellationToken);

            return owner is null
                ? TypedResults.NotFound(new ProblemDetails
                {
                    Title = "SKU not found",
                    Detail = $"SKU '{sku}' is not assigned to a product variant."
                })
                : TypedResults.Ok(new ProductSkuOwnerResponse(
                    owner.ProductId.Value,
                    owner.ProductVariantId.Value));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid SKU",
                Detail = exception.Message
            });
        }
    }
}

public sealed record ProductSkuOwnerResponse(Guid ProductId, Guid ProductVariantId);
