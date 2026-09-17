using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog;
using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.SetVariantAttributeValue;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.SetVariantAttributeValue.SetVariantAttributeValue;

namespace MyShop.Api.Catalog.Products;

public static class SetVariantAttributeValueEndpoint
{
    public static IEndpointRouteBuilder MapSetVariantAttributeValue(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapPut(
                "/api/products/{productId:guid}/variants/{variantId:guid}/attributes/{attributeDefinitionId:guid}",
                ExecuteAsync)
            .WithName("SetVariantAttributeValue");

        return endpoints;
    }

    public static async Task<Results<
        NoContent,
        NotFound<ProblemDetails>,
        BadRequest<ProblemDetails>,
        Conflict<ProblemDetails>>> ExecuteAsync(
        Guid productId,
        Guid variantId,
        Guid attributeDefinitionId,
        SetVariantAttributeValueRequest request,
        [FromServices] UseCase useCase,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(useCase);

        try
        {
            var result = await useCase.ExecuteAsync(
                new SetVariantAttributeValueCommand(
                    ProductId.From(productId),
                    ProductVariantId.From(variantId),
                    AttributeDefinitionId.From(attributeDefinitionId),
                    MapValue(request)),
                cancellationToken);

            return result.Failure switch
            {
                SetVariantAttributeValueFailure.ProductNotFound => NotFound(
                    "Product not found", $"Product '{productId}' does not exist."),
                SetVariantAttributeValueFailure.VariantNotFound => NotFound(
                    "Product variant not found", $"Product variant '{variantId}' does not exist."),
                SetVariantAttributeValueFailure.ProductTypeNotFound => NotFound(
                    "Product type not found", "The product type does not exist."),
                SetVariantAttributeValueFailure.AttributeDefinitionNotFound => NotFound(
                    "Attribute definition not found",
                    $"Attribute definition '{attributeDefinitionId}' does not exist on the product type."),
                SetVariantAttributeValueFailure.WrongAttributeScope => Conflict(
                    "Wrong attribute scope", "The attribute definition is not scoped to variants."),
                SetVariantAttributeValueFailure.WrongAttributeDataType => Conflict(
                    "Wrong attribute data type",
                    $"The attribute definition does not accept '{request.DataType}' values."),
                null => TypedResults.NoContent(),
                _ => throw new InvalidOperationException(
                    $"Set variant attribute failure '{result.Failure}' is not supported.")
            };
        }
        catch (ProductConcurrencyException exception)
        {
            return Conflict("Product was modified", exception.Message);
        }
        catch (ArgumentException exception)
        {
            return TypedResults.BadRequest(new ProblemDetails
            {
                Title = "Invalid product variant attribute value",
                Detail = exception.Message
            });
        }
    }

    private static CatalogAttributeValueInput MapValue(SetVariantAttributeValueRequest request) =>
        request.DataType switch
        {
            AttributeDataType.Text => new TextAttributeValueInput(ReadString(request.Value, "text")),
            AttributeDataType.Integer => new IntegerAttributeValueInput(ReadInteger(request.Value)),
            AttributeDataType.Decimal => new DecimalAttributeValueInput(ReadDecimal(request.Value)),
            AttributeDataType.Boolean => new BooleanAttributeValueInput(ReadBoolean(request.Value)),
            AttributeDataType.Date => new DateAttributeValueInput(ReadDate(request.Value)),
            AttributeDataType.Choice => new ChoiceAttributeValueInput(ReadString(request.Value, "choice")),
            AttributeDataType.MultiChoice => new MultiChoiceAttributeValueInput(ReadStrings(request.Value)),
            _ => throw new ArgumentException(
                $"Attribute data type '{request.DataType}' is not supported.", nameof(request))
        };

    private static string ReadString(JsonElement value, string description)
    {
        if (value.ValueKind != JsonValueKind.String)
            throw InvalidValue(description);
        return value.GetString()!;
    }

    private static long ReadInteger(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt64(out var result))
            throw InvalidValue("integer");
        return result;
    }

    private static decimal ReadDecimal(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Number || !value.TryGetDecimal(out var result))
            throw InvalidValue("decimal");
        return result;
    }

    private static bool ReadBoolean(JsonElement value)
    {
        if (value.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw InvalidValue("boolean");
        return value.GetBoolean();
    }

    private static DateOnly ReadDate(JsonElement value)
    {
        var text = ReadString(value, "date");
        if (!DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var result))
            throw InvalidValue("date in yyyy-MM-dd format");
        return result;
    }

    private static IReadOnlyList<string> ReadStrings(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.Array)
            throw InvalidValue("array of choices");

        var result = new List<string>();
        foreach (var item in value.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
                throw InvalidValue("array of choices");
            result.Add(item.GetString()!);
        }
        return result;
    }

    private static ArgumentException InvalidValue(string expected) =>
        new($"Value must be a valid {expected}.", "request");

    private static NotFound<ProblemDetails> NotFound(string title, string detail) =>
        TypedResults.NotFound(new ProblemDetails { Title = title, Detail = detail });

    private static Conflict<ProblemDetails> Conflict(string title, string detail) =>
        TypedResults.Conflict(new ProblemDetails { Title = title, Detail = detail });
}

public sealed record SetVariantAttributeValueRequest(
    AttributeDataType DataType,
    JsonElement Value);
