namespace MyShop.Domain.Catalog;

public sealed record ProductPresentation
{
    private ProductPresentation(string description, string? imageUrl, string imageAlt, bool isPublished)
    {
        Description = description;
        ImageUrl = imageUrl;
        ImageAlt = imageAlt;
        IsPublished = isPublished;
    }
    public string Description { get; }
    public string? ImageUrl { get; }
    public string ImageAlt { get; }
    public bool IsPublished { get; }
    public static ProductPresentation Draft { get; } = new("", null, "", false);

    public static ProductPresentation Create(string description, string? imageUrl, string imageAlt, bool isPublished)
    {
        ArgumentNullException.ThrowIfNull(description);
        ArgumentNullException.ThrowIfNull(imageAlt);
        description = description.Trim();
        imageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        imageAlt = imageAlt.Trim();
        if (description.Length > 10000) throw new ArgumentException("Description must not exceed 10000 characters.", nameof(description));
        if (imageAlt.Length > 250) throw new ArgumentException("Image alternative text must not exceed 250 characters.", nameof(imageAlt));
        if (imageUrl is not null)
        {
            var uploaded = imageUrl.StartsWith("/api/shop/product-images/", StringComparison.Ordinal) &&
                Guid.TryParseExact(imageUrl["/api/shop/product-images/".Length..], "D", out var imageId) && imageId != Guid.Empty;
            if (!uploaded && (imageUrl.Length > 2048 || imageUrl.Any(char.IsControl) || imageUrl.Contains('\\') ||
                !Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps ||
                string.IsNullOrEmpty(uri.Host) || !string.IsNullOrEmpty(uri.UserInfo)))
                throw new ArgumentException("Use an absolute HTTPS image URL without credentials.", nameof(imageUrl));
            if (imageAlt.Length == 0) throw new ArgumentException("An image requires alternative text.", nameof(imageAlt));
        }
        else if (imageAlt.Length != 0)
            throw new ArgumentException("Alternative text requires an image.", nameof(imageAlt));
        if (isPublished && (description.Length == 0 || imageUrl is null))
            throw new ArgumentException("Publishing requires a description and image.", nameof(isPublished));
        return new(description, imageUrl, imageAlt, isPublished);
    }
}
