using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using HDWebFramework.Application.Catalog.Seo;

namespace HDWebFramework.Api.Catalog;

public static class ShopIconEndpoints
{
    public static void MapShopIcon(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/shop-icon", Upload).WithMetadata(new RequestSizeLimitAttribute(ManageSeo.MaximumIconBytes + 65536));
        endpoints.MapDelete("/api/shop-icon", async ([FromBody] IconRevision request, ManageSeo useCase, CancellationToken ct) =>
            await Change(() => useCase.SaveIconAsync(null, "", request.Revision, ct)));
        endpoints.MapGet("/api/shop/icon", async (HttpContext context, ManageSeo useCase, CancellationToken ct) =>
        {
            var icon = await useCase.GetIconAsync(ct);
            if (icon is null) return Results.NotFound();
            context.Response.Headers.XContentTypeOptions = "nosniff";
            context.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(icon.Bytes, icon.ContentType);
        }).AllowAnonymous();
    }
    private static Task<IResult> Upload(HttpRequest request, ManageSeo useCase, CancellationToken ct) => Change(async () =>
    {
        var size = request.HttpContext.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (size?.IsReadOnly == false) size.MaxRequestBodySize = ManageSeo.MaximumIconBytes + 65536;
        if (!request.HasFormContentType) throw new ArgumentException("Kies een JPEG- of PNG-afbeelding.");
        var form = await request.ReadFormAsync(new FormOptions { MultipartBodyLengthLimit = ManageSeo.MaximumIconBytes, ValueLengthLimit = 256, ValueCountLimit = 4 }, ct);
        if (form.Files.Count != 1 || !Guid.TryParse(form["revision"], out var revision))
            throw new ArgumentException("Kies één icoon en vernieuw zo nodig de instellingen.");
        var file = form.Files[0];
        if (file.Length > ManageSeo.MaximumIconBytes) throw new ArgumentException("Kies een icoon van maximaal 1 MB.");
        using var buffer = new MemoryStream();
        await file.CopyToAsync(buffer, ct);
        return await useCase.SaveIconAsync(buffer.ToArray(), file.FileName, revision, ct);
    });
    private static async Task<IResult> Change(Func<Task<SeoResult>> action)
    {
        try
        {
            var result = await action();
            return result.Failure is null ? Results.NoContent() : (IResult)Results.Conflict(new { code = "concurrency", message = "De webshopinstellingen zijn gewijzigd. Haal de gegevens opnieuw op." });
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidDataException or BadHttpRequestException)
        { return Results.BadRequest(new { code = "invalidShopIcon", message = "Kies een volledige JPEG- of PNG-afbeelding van maximaal 1 MB en 20 miljoen pixels." }); }
    }
}
public sealed record IconRevision(Guid Revision);
