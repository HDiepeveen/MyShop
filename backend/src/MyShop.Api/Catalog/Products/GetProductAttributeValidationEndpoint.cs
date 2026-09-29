using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.GetProductAttributeValidation;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductAttributeValidation.GetProductAttributeValidation;

namespace MyShop.Api.Catalog.Products;

public static class GetProductAttributeValidationEndpoint
{
    public static IEndpointRouteBuilder MapGetProductAttributeValidation(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapGet("/api/products/{productId:guid}/attribute-validation", ExecuteAsync)
            .WithName("GetProductAttributeValidation");
        return endpoints;
    }

    public static async Task<Results<Ok<ProductAttributeValidationResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> ExecuteAsync(
        Guid productId, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(useCase);
        ProductId id;
        try
        {
            id = ProductId.From(productId);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails { Title = "Invalid product ID", Detail = exception.Message });
        }

        var result = await useCase.ExecuteAsync(new(id), cancellationToken);
        if (result.Failure == GetProductAttributeValidationFailure.ProductNotFound)
            return TypedResults.NotFound(new ProblemDetails { Title = "Product not found", Detail = $"Product '{productId}' does not exist." });
        if (result.Failure == GetProductAttributeValidationFailure.ProductTypeNotFound)
            return TypedResults.NotFound(new ProblemDetails { Title = "Product type not found", Detail = "The product's type does not exist." });
        var issues = result.Issues
            ?? throw new InvalidOperationException("A successful validation query must contain issues.");
        return TypedResults.Ok(new ProductAttributeValidationResponse(productId, issues.Count == 0,
            issues.Select(issue => new ProductAttributeIssueResponse(
                issue.AttributeDefinitionId.Value, issue.VariantId?.Value, issue.Code.ToString())).ToArray()));
    }
}

public sealed record ProductAttributeValidationResponse(
    Guid ProductId, bool IsValid, IReadOnlyList<ProductAttributeIssueResponse> Issues);

public sealed record ProductAttributeIssueResponse(Guid AttributeDefinitionId, Guid? VariantId, string Code);
