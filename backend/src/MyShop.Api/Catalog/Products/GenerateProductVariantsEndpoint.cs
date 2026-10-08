using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GenerateProductVariants;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GenerateProductVariants.GenerateProductVariants;

namespace MyShop.Api.Catalog.Products;

public static class GenerateProductVariantsEndpoint
{
    public static IEndpointRouteBuilder MapGenerateProductVariants(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/products/{productId:guid}/variant-combinations", ExecuteAsync)
            .WithName("GenerateProductVariants");
        return endpoints;
    }

    public static async Task<Results<Ok<GenerateProductVariantsResult>, NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId, GenerateProductVariantsRequest request, [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        try
        {
            if (request.Combinations is null || request.Combinations.Count is < 1 or > 100
                || request.Combinations.Any(c => c is null || c.Values is null || c.Values.Count is < 1 or > 10
                    || c.Values.Any(v => v is null || v.Value is null)))
                throw new ArgumentException("Kies tussen 1 en 100 combinaties met 1 tot 10 kenmerken.");
            var combinations = request.Combinations.Select(c => new VariantCombination(c.Name,
                c.Values.Select(v => new VariantCombinationValue(AttributeDefinitionId.From(v.AttributeDefinitionId),
                    AttributeValueRequestMapper.Map(v.Value))).ToArray())).ToArray();
            var result = await useCase.ExecuteAsync(new(ProductId.From(productId), request.Revision, combinations,
                request.NetAmount, request.VatRate, request.VatExempt, request.StockQuantity), cancellationToken);
            if (result.Failure is not null)
                return TypedResults.NotFound(new ProblemDetails { Title = "Product not found", Detail = "Het product of producttype bestaat niet meer." });
            return TypedResults.Ok(result);
        }
        catch (ProductConcurrencyException)
        {
            return TypedResults.Conflict(new ProblemDetails { Title = "Product was modified", Detail = "Het product is ondertussen gewijzigd. Vernieuw de pagina en controleer de combinaties." });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Invalid variant combinations", Detail = exception.Message });
        }
    }
}

public sealed record GenerateProductVariantsRequest(Guid Revision, IReadOnlyList<VariantCombinationRequest> Combinations,
    decimal? NetAmount = null, decimal? VatRate = null, bool VatExempt = false, int? StockQuantity = null);
public sealed record VariantCombinationRequest(string Name, IReadOnlyList<VariantCombinationValueRequest> Values);
public sealed record VariantCombinationValueRequest(Guid AttributeDefinitionId, AttributeValueRequest Value);
