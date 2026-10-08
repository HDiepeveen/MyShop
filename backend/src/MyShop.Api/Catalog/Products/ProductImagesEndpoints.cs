using MyShop.Application.Catalog.ManageProductImages;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MyShop.Api.Security;
using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Api.Catalog.Products;

public static class ProductImagesEndpoints
{
    public static void MapProductImages(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/products/{productId:guid}/images", async (Guid productId, ManageProductImages images, CancellationToken ct) =>
            await images.ExecuteAsync(productId, ct) is { } result ? Results.Ok(result) : Results.NotFound());
        app.MapPost("/api/products/{productId:guid}/images", Upload)
            .WithMetadata(new RequestSizeLimitAttribute(51 * 1024 * 1024));
        app.MapPut("/api/products/{productId:guid}/images/{imageId:guid}/main", async (Guid productId, Guid imageId,
            ImageRevision request, ManageProductImages images, CancellationToken ct) =>
            await Change(() => images.SetMainAsync(productId, imageId, request.Revision, ct)));
        app.MapDelete("/api/products/{productId:guid}/images/{imageId:guid}", async (Guid productId, Guid imageId,
            [FromBody] ImageRevision request, ManageProductImages images, CancellationToken ct) =>
            await Change(() => images.DeleteAsync(productId, imageId, request.Revision, ct)));
        app.MapGet("/api/shop/product-images/{imageId:guid}", async (Guid imageId, HttpContext context, ManageProductImages images, CancellationToken ct) =>
        {
            var image = await images.ContentAsync(imageId, context.User.IsInRole(AdminSecurity.Role), ct);
            if (image is null) return Results.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(image.Bytes, image.ContentType);
        }).AllowAnonymous();
    }

    private static async Task<IResult> Upload(Guid productId, HttpRequest request, ManageProductImages images, CancellationToken ct)
    {
        return await Change(async () =>
        {
            var size = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
            if (size?.IsReadOnly == false) size.MaxRequestBodySize = 51 * 1024 * 1024;
            if (!request.HasFormContentType) throw new ArgumentException("A multipart form is required.");
            var form = await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = ProductImageUpload.MaximumBytes,
                ValueLengthLimit = 1024, ValueCountLimit = 16 }, ct);
            if (!Guid.TryParse(form["revision"], out var revision) || form.Files.Count is < 1 or > ProductImageUpload.MaximumImages)
                throw new ArgumentException("Choose 1 to 10 images and provide the revision.");
            var uploads = new List<ProductImageUpload>();
            foreach (var file in form.Files)
            {
                if (file.Length is < 1 or > ProductImageUpload.MaximumBytes) throw new ArgumentException("Image too large.");
                using var buffer = new MemoryStream();
                await file.CopyToAsync(buffer, ct);
                uploads.Add(ProductImageUpload.Create(buffer.ToArray(), form["alternativeText"].ToString(), file.FileName));
            }
            return await images.UploadAsync(productId, revision, uploads, ct);
        });
    }

    private static async Task<IResult> Change(Func<Task<bool>> action)
    {
        try { return await action() ? Results.NoContent() : Results.NotFound(); }
        catch (ProductConcurrencyException) { return Results.Conflict(new { message = "Het product is gewijzigd. Vernieuw de gegevens en probeer opnieuw." }); }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or BadHttpRequestException)
        { return Results.BadRequest(new { message = "Kies JPEG- of PNG-foto’s van maximaal 5 MB en 20 miljoen pixels, maximaal 10 per product. Vul ook alternatieve tekst in." }); }
    }
}

public sealed record ImageRevision(Guid Revision);
