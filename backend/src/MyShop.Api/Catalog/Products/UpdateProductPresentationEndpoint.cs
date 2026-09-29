using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.UpdateProductPresentation;
using MyShop.Domain.Catalog;

namespace MyShop.Api.Catalog.Products;

public static class UpdateProductPresentationEndpoint
{
    public static IEndpointRouteBuilder MapUpdateProductPresentation(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPut("/api/products/{productId:guid}/presentation", ExecuteAsync).WithName("UpdateProductPresentation");
        return endpoints;
    }
    public static async Task<IResult> ExecuteAsync(Guid productId, ProductPresentationRequest request,
        [FromServices] UpdateProductPresentation useCase, CancellationToken cancellationToken)
    {
        try
        {
            var presentation = ProductPresentation.Create(request.Description, request.ImageUrl, request.ImageAlt, request.IsPublished);
            var found = await useCase.ExecuteAsync(new(ProductId.From(productId), request.Revision, presentation), cancellationToken);
            return found ? Results.NoContent() : Results.NotFound();
        }
        catch (ProductConcurrencyException) { return Results.Conflict(new { message = "Het product is gewijzigd. Vernieuw de gegevens en probeer opnieuw." }); }
        catch (ArgumentException) { return Results.BadRequest(new { message = "Controleer de beschrijving, HTTPS-afbeeldingslink en alternatieve tekst. Publiceren vereist een beschrijving en afbeelding." }); }
    }
}
public sealed record ProductPresentationRequest(string Description, string? ImageUrl, string ImageAlt, bool IsPublished, Guid Revision);
