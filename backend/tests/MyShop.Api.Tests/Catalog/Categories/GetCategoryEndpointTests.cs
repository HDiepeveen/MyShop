using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using MyShop.Api.Catalog.Categories;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetCategory.GetCategory;

namespace MyShop.Api.Tests.Catalog.Categories;

public sealed class GetCategoryEndpointTests
{
    [Fact]
    public void MapGetCategory_MapsNamedGetRoute()
    {
        var builder = WebApplication.CreateBuilder();
        var app = builder.Build();

        Assert.Same(app, app.MapGetCategory());
        var routes = (IEndpointRouteBuilder)app;
        var endpoint = Assert.IsType<RouteEndpoint>(Assert.Single(routes.DataSources).Endpoints.Single());
        Assert.Equal("/api/categories/{categoryId:guid}", endpoint.RoutePattern.RawText);
        Assert.Equal("GetCategory", endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()!.EndpointName);
        Assert.Equal(["GET"], endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()!.HttpMethods);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ExecuteAsync_WhenCategoryExists_ReturnsCompleteResponse(bool isRoot)
    {
        var scenario = new Scenario(isRoot);

        var result = await scenario.ExecuteAsync();

        var ok = Assert.IsType<Ok<GetCategoryResponse>>(result.Result);
        var response = Assert.IsType<GetCategoryResponse>(ok.Value);
        Assert.Equal(scenario.Category.Id.Value, response.Id);
        Assert.Equal("Clothing", response.Name);
        Assert.Equal(scenario.Category.ParentCategoryId?.Value, response.ParentCategoryId);
        Assert.Equal(isRoot, response.IsRoot);
    }

    [Fact]
    public async Task ExecuteAsync_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var scenario = new Scenario(true) { CategoryIsMissing = true };

        var result = await scenario.ExecuteAsync();

        var notFound = Assert.IsType<NotFound<ProblemDetails>>(result.Result);
        Assert.Equal("Category not found", notFound.Value!.Title);
    }

    [Fact]
    public async Task ExecuteAsync_WhenIdIsInvalid_ReturnsBadRequestWithoutRepositoryAccess()
    {
        var scenario = new Scenario(true);

        var result = await GetCategoryEndpoint.ExecuteAsync(
            Guid.Empty,
            scenario.UseCase,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequest<ProblemDetails>>(result.Result);
        Assert.Equal("Invalid category ID", badRequest.Value!.Title);
        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    [Fact]
    public async Task ExecuteAsync_ForwardsCancellation()
    {
        var scenario = new Scenario(true);
        using var source = new CancellationTokenSource();
        source.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            scenario.ExecuteAsync(source.Token));

        Assert.Equal(0, scenario.Repository.GetCalls);
    }

    private sealed class Scenario
    {
        public Scenario(bool isRoot)
        {
            Category = isRoot
                ? MyShop.Domain.Catalog.Category.CreateRoot("Clothing")
                : MyShop.Domain.Catalog.Category.CreateChild("Clothing", CategoryId.New());
            Repository = new CategoryRepositoryFake(this);
            UseCase = new UseCase(Repository);
        }

        public Category Category { get; }
        public bool CategoryIsMissing { get; set; }
        public CategoryRepositoryFake Repository { get; }
        public UseCase UseCase { get; }

        public Task<Results<Ok<GetCategoryResponse>, NotFound<ProblemDetails>, BadRequest<ProblemDetails>>>
            ExecuteAsync(CancellationToken cancellationToken = default) =>
            GetCategoryEndpoint.ExecuteAsync(
                Category.Id.Value,
                UseCase,
                cancellationToken);
    }

    private sealed class CategoryRepositoryFake(Scenario scenario) : ICategoryRepository
    {
        public int GetCalls { get; private set; }

        public Task<Category?> GetByIdAsync(CategoryId id, CancellationToken cancellationToken)
        {
            GetCalls++;
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(scenario.CategoryIsMissing ? null : scenario.Category);
        }
    }
}
