using System.Net;
using HDWebFramework.Api.Tests.Security;

namespace HDWebFramework.Api.Tests.Catalog.Seo;

public sealed class LegalPageHttpTests
{
    [SecuritySqlFact]
    public async Task Legal_documents_are_public_on_direct_navigation_and_drafts_are_not_indexed()
    {
        var folder = Path.Combine(Path.GetTempPath(), "HDWebFrameworkLegal_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "legal"));
        await File.WriteAllTextAsync(Path.Combine(folder, "index.html"), "<html><head><title>Website</title></head><body><app-root></app-root></body></html>");
        var slugs = new[] { "algemene-voorwaarden", "privacyverklaring", "cookiebeleid" };
        foreach (var slug in slugs)
            await File.WriteAllTextAsync(Path.Combine(folder, "legal", slug + ".html"), "<p>Popovstraat 7-B — " + slug + "</p>");
        try
        {
            await using var host = await SecurityHost.Create(new() { ["Hosting:FrontendRoot"] = folder });
            using var visitor = new HttpClient { BaseAddress = host.Client.BaseAddress };
            foreach (var slug in slugs)
            {
                var response = await visitor.GetAsync("/" + slug);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
                Assert.Contains("noindex", response.Headers.GetValues("X-Robots-Tag"));
                var html = await response.Content.ReadAsStringAsync();
                Assert.Contains("Popovstraat 7-B", html);
                Assert.Contains("Autoservice Ganpat", html);
                Assert.Contains(slug, html);
            }
        }
        finally { Directory.Delete(folder, true); }
    }
}
