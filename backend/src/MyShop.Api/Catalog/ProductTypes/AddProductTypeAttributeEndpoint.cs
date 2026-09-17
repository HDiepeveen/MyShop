using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.AddProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.AddProductTypeAttribute.AddProductTypeAttribute;

namespace MyShop.Api.Catalog.ProductTypes;

public static class AddProductTypeAttributeEndpoint
{
    public static IEndpointRouteBuilder MapAddProductTypeAttribute(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);
        endpoints.MapPost("/api/product-types/{productTypeId:guid}/attributes", ExecuteAsync)
            .WithName("AddProductTypeAttribute");
        return endpoints;
    }

    public static async Task<Results<
        Created<AddProductTypeAttributeResponse>, NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>, Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productTypeId,
        AddProductTypeAttributeRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);
        try
        {
            var attribute = await useCase.ExecuteAsync(new AddProductTypeAttributeCommand(
                ProductTypeId.From(productTypeId), request.Code, request.DisplayName,
                request.DataType, request.IsRequired, request.IsFilterable, request.Scope),
                cancellationToken);
            if (attribute is null)
                return TypedResults.NotFound(new ProblemDetails { Title = "Product type not found" });

            var response = new AddProductTypeAttributeResponse(
                attribute.Id.Value, attribute.Code.Value, attribute.DisplayName,
                MapDataType(attribute.DataType), attribute.IsRequired, attribute.IsFilterable,
                MapScope(attribute.Scope));
            return TypedResults.Created(
                $"/api/product-types/{productTypeId}/attributes/{attribute.Id.Value}", response);
        }
        catch (InvalidOperationException exception)
        {
            return TypedResults.Conflict(new ProblemDetails
            {
                Title = "Product type attribute conflict", Detail = exception.Message
            });
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product type attribute", Detail = exception.Message
            });
        }
    }

    private static AttributeDataTypeResponse MapDataType(AttributeDataType dataType) => dataType switch
    {
        AttributeDataType.Text => AttributeDataTypeResponse.Text,
        AttributeDataType.Integer => AttributeDataTypeResponse.Integer,
        AttributeDataType.Decimal => AttributeDataTypeResponse.Decimal,
        AttributeDataType.Boolean => AttributeDataTypeResponse.Boolean,
        AttributeDataType.Date => AttributeDataTypeResponse.Date,
        AttributeDataType.Choice => AttributeDataTypeResponse.Choice,
        AttributeDataType.MultiChoice => AttributeDataTypeResponse.MultiChoice,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unsupported attribute data type.")
    };

    private static AttributeScopeResponse MapScope(AttributeScope scope) => scope switch
    {
        AttributeScope.Product => AttributeScopeResponse.Product,
        AttributeScope.Variant => AttributeScopeResponse.Variant,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported attribute scope.")
    };
}

public sealed record AddProductTypeAttributeRequest(
    string Code, string DisplayName, AttributeDataType DataType,
    bool IsRequired, bool IsFilterable, AttributeScope Scope);

public sealed record AddProductTypeAttributeResponse(
    Guid Id, string Code, string DisplayName, AttributeDataTypeResponse DataType,
    bool IsRequired, bool IsFilterable, AttributeScopeResponse Scope);

public enum AttributeDataTypeResponse
{
    Text = 0,
    Integer = 1,
    Decimal = 2,
    Boolean = 3,
    Date = 4,
    Choice = 5,
    MultiChoice = 6
}

public enum AttributeScopeResponse
{
    Product = 0,
    Variant = 1
}
