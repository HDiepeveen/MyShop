using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.CreateCategory;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.CreateCategory.CreateCategory;

namespace MyShop.Api.Catalog.Categories;

public static class CreateCategoryEndpoint
{
    public static IEndpointRouteBuilder MapCreateCategory(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/categories", ExecuteAsync).WithName("CreateCategory");
        return endpoints;
    }

    public static async Task<Results<
        Created<CreateCategoryResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>> ExecuteAsync(
        CreateCategoryRequest request, [FromServices] UseCase useCase, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var result = await useCase.ExecuteAsync(new(
                request.Name,
                request.ParentCategoryId is null ? null : CategoryId.From(request.ParentCategoryId.Value)),
                cancellationToken);
            if (result.ParentNotFound)
                return TypedResults.NotFound(new ProblemDetails
                {
                    Title = "Parent category not found",
                    Detail = $"Category '{request.ParentCategoryId}' does not exist."
                });

            var category = result.Category
                ?? throw new InvalidOperationException("A successful create result must contain a category.");
            return TypedResults.Created($"/api/categories/{category.Id.Value}",
                new CreateCategoryResponse(category.Id.Value, category.Name, category.ParentCategoryId?.Value));
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid category", Detail = exception.Message
            });
        }
    }
}

public sealed record CreateCategoryRequest(string Name, Guid? ParentCategoryId = null);
public sealed record CreateCategoryResponse(Guid Id, string Name, Guid? ParentCategoryId);
