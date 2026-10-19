using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using MyShop.Application.Catalog.Seo;

namespace MyShop.Api.Catalog;

public static class HomeImageEndpoints
{
    public static void MapHomeImage(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/home-image", Upload).WithMetadata(new RequestSizeLimitAttribute(ManageSeo.MaximumHomeImageBytes + 65536));
        endpoints.MapDelete("/api/home-image", async ([FromBody] HomeImageRevision request, ManageSeo useCase, CancellationToken ct) =>
            await Change(() => useCase.SaveHomeImageAsync(null, "", request.Revision, ct)));
        endpoints.MapGet("/api/shop/home-image", async (HttpContext context, ManageSeo useCase, CancellationToken ct) =>
        {
            var image = await useCase.GetHomeImageAsync(ct);
            if (image is null) return Results.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(image.Bytes, image.ContentType);
        }).AllowAnonymous();
    }
    private static Task<IResult> Upload(HttpRequest request, ManageSeo useCase, CancellationToken ct) => Change(async () =>
    {
        var size = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (size?.IsReadOnly == false) size.MaxRequestBodySize = ManageSeo.MaximumHomeImageBytes + 65536;
        if (!request.HasFormContentType) throw new ArgumentException("Kies een JPEG- of PNG-afbeelding.");
        var form = await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = ManageSeo.MaximumHomeImageBytes, ValueLengthLimit = 256, ValueCountLimit = 4 }, ct);
        if (form.Files.Count != 1 || !Guid.TryParse(form["revision"], out var revision))
            throw new ArgumentException("Kies één afbeelding en vernieuw zo nodig de instellingen.");
        var file = form.Files[0];
        if (file.Length > ManageSeo.MaximumHomeImageBytes) throw new ArgumentException("Kies een afbeelding van maximaal 5 MB.");
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return await useCase.SaveHomeImageAsync(buffer.ToArray(), file.FileName, revision, ct);
    });
    private static async Task<IResult> Change(Func<Task<SeoResult>> action)
    {
        try
        {
            var result = await action();
            return result.Failure is null ? Results.NoContent() : (IResult)Results.Conflict(new { code = "concurrency", message = "De webshopinstellingen zijn gewijzigd. Haal de gegevens opnieuw op." });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or BadHttpRequestException)
        { return Results.BadRequest(new { code = "invalidHomeImage", message = "Kies een volledige JPEG- of PNG-afbeelding van maximaal 5 MB en 20 miljoen pixels." }); }
    }
}
public sealed record HomeImageRevision(Guid Revision);
