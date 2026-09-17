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
                MapRequestDataType(request.DataType), request.IsRequired, request.IsFilterable,
                MapRequestScope(request.Scope)),
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

    private static ApiAttributeDataType MapDataType(AttributeDataType dataType) => dataType switch
    {
        AttributeDataType.Text => ApiAttributeDataType.Text,
        AttributeDataType.Integer => ApiAttributeDataType.Integer,
        AttributeDataType.Decimal => ApiAttributeDataType.Decimal,
        AttributeDataType.Boolean => ApiAttributeDataType.Boolean,
        AttributeDataType.Date => ApiAttributeDataType.Date,
        AttributeDataType.Choice => ApiAttributeDataType.Choice,
        AttributeDataType.MultiChoice => ApiAttributeDataType.MultiChoice,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unsupported attribute data type.")
    };

    private static AttributeDataType MapRequestDataType(ApiAttributeDataType dataType) => dataType switch
    {
        ApiAttributeDataType.Text => AttributeDataType.Text,
        ApiAttributeDataType.Integer => AttributeDataType.Integer,
        ApiAttributeDataType.Decimal => AttributeDataType.Decimal,
        ApiAttributeDataType.Boolean => AttributeDataType.Boolean,
        ApiAttributeDataType.Date => AttributeDataType.Date,
        ApiAttributeDataType.Choice => AttributeDataType.Choice,
        ApiAttributeDataType.MultiChoice => AttributeDataType.MultiChoice,
        _ => throw new ArgumentOutOfRangeException(nameof(dataType), dataType, "Unsupported attribute data type.")
    };

    private static ApiAttributeScope MapScope(AttributeScope scope) => scope switch
    {
        AttributeScope.Product => ApiAttributeScope.Product,
        AttributeScope.Variant => ApiAttributeScope.Variant,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported attribute scope.")
    };

    private static AttributeScope MapRequestScope(ApiAttributeScope scope) => scope switch
    {
        ApiAttributeScope.Product => AttributeScope.Product,
        ApiAttributeScope.Variant => AttributeScope.Variant,
        _ => throw new ArgumentOutOfRangeException(nameof(scope), scope, "Unsupported attribute scope.")
    };
}

public sealed record AddProductTypeAttributeRequest(
    string Code, string DisplayName, ApiAttributeDataType DataType,
    bool IsRequired, bool IsFilterable, ApiAttributeScope Scope);

public sealed record AddProductTypeAttributeResponse(
    Guid Id, string Code, string DisplayName, ApiAttributeDataType DataType,
    bool IsRequired, bool IsFilterable, ApiAttributeScope Scope);
