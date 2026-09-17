using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.CreateProductType;
using UseCase = MyShop.Application.Catalog.CreateProductType.CreateProductType;

namespace MyShop.Api.Catalog.ProductTypes;

public static class CreateProductTypeEndpoint
{
    public static IEndpointRouteBuilder MapCreateProductType(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/product-types", ExecuteAsync).WithName("CreateProductType");
        return endpoints;
    }

    public static async Task<Results<Created<CreateProductTypeResponse>, BadRequest<ProblemDetails>>>
        ExecuteAsync(
            CreateProductTypeRequest request,
            [FromServices] UseCase useCase,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var productType = await useCase.ExecuteAsync(
                new CreateProductTypeCommand(request.Name), cancellationToken);
            var response = new CreateProductTypeResponse(productType.Id.Value, productType.Name);
            return TypedResults.Created($"/api/product-types/{productType.Id.Value}", response);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type",
                Detail = exception.Message
            });
        }
    }
}

public sealed record CreateProductTypeRequest(string Name);
public sealed record CreateProductTypeResponse(Guid Id, string Name);
