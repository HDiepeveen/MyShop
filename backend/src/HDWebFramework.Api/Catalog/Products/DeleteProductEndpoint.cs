using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Domain.Catalog;
using UseCase = HDWebFramework.Application.Catalog.DeleteProduct.DeleteProduct;

namespace HDWebFramework.Api.Catalog.Products;

public static class DeleteProductEndpoint
{
    public static IEndpointRouteBuilder MapDeleteProduct(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapDelete("/api/products/{productId:guid}", ExecuteAsync).WithName("DeleteProduct");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(Guid productId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            return await useCase.ExecuteAsync(ProductId.From(productId), cancellationToken)
                ? TypedResults.NoContent()
                : TypedResults.NotFound(new ProblemDetails { Title = "Product not found" });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
                { Title = "Invalid product deletion", Detail = exception.Message });
        }
    }
}
