using MyShop.Application.Notifications;

namespace MyShop.Api;

public static class EmailSettingsEndpoints
{
    public static void MapEmailSettings(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/email-settings", async (GetEmailSettings useCase, CancellationToken token) =>
            Results.Ok(await useCase.ExecuteAsync(token)));
        endpoints.MapPut("/api/email-settings", async (UpdateEmailSettingsCommand request, UpdateEmailSettings useCase,
            IHostEnvironment environment, CancellationToken token) =>
        {
            try
            {
                var saved = await useCase.ExecuteAsync(request, environment.IsDevelopment(), token);
                return saved is null ? Results.Conflict(new { code = "concurrency", message = "De e-mailinstellingen zijn gewijzigd. Vernieuw de pagina." }) : Results.Ok(saved);
            }
            catch (ArgumentException exception) { return Results.BadRequest(new { code = "invalidEmailSettings", message = exception.Message }); }
        });
        endpoints.MapPost("/api/email-settings/test", async (EmailTestRequest request, SendEmailTest useCase, CancellationToken token) =>
        {
            try
            {
                return await useCase.ExecuteAsync(request.Recipient, request.Revision, token)
                    ? Results.Accepted(value: new { message = "Testmail klaargezet. Controleer je mailbox." })
                    : Results.Conflict(new { code = "emailNotReady", message = "Sla de instellingen op en schakel verzending in. Vernieuw bij gewijzigde instellingen." });
            }
            catch (ArgumentException exception) { return Results.BadRequest(new { code = "invalidEmailSettings", message = exception.Message }); }
        }).RequireRateLimiting("admin-login");
    }
}

public sealed record EmailTestRequest(string Recipient, Guid Revision);
