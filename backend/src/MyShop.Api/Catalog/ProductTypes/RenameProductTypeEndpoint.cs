using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.RenameProductType;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.RenameProductType.RenameProductType;

namespace MyShop.Api.Catalog.ProductTypes;

public static class RenameProductTypeEndpoint
{
    public static IEndpointRouteBuilder MapRenameProductType(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPatch("/api/product-types/{productTypeId:guid}/name", ExecuteAsync)
            .WithName("RenameProductType");
        return endpoints;
    }

    public static async Task<Results<NoContent, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
        ExecuteAsync(
            Guid productTypeId,
            RenameProductTypeRequest request,
            [FromServices] UseCase useCase,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var found = await useCase.ExecuteAsync(
                new RenameProductTypeCommand(ProductTypeId.From(productTypeId), request.Name),
                cancellationToken);
            return found
                ? TypedResults.NoContent()
                : TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Product type not found",
                    Detail = $"Product type '{productTypeId}' does not exist."
                });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type rename",
                Detail = exception.Message
            });
        }
    }
}

public sealed record RenameProductTypeRequest(string Name);
