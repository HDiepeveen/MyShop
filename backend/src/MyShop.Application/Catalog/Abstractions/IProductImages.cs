namespace MyShop.Application.Catalog.Abstractions;

public sealed record ProductImageInfo(Guid Id, string Url, string AlternativeText, string FileName);
public sealed record ProductImageContent(byte[] Bytes, string ContentType);
public sealed record ProductImages(Guid Revision, string? MainImageUrl, IReadOnlyList<ProductImageInfo> Images);

public interface IProductImages
{
    Task<ProductImages?> GetAsync(Guid productId, CancellationToken cancellationToken);
    Task<ProductImageContent?> ContentAsync(Guid imageId, bool administrator, CancellationToken cancellationToken);
    Task<bool> UploadAsync(Guid productId, Guid revision, IReadOnlyList<ProductImageUpload> images, CancellationToken cancellationToken);
    Task<bool> SetMainAsync(Guid productId, Guid imageId, Guid revision, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid productId, Guid imageId, Guid revision, CancellationToken cancellationToken);
    Task<bool> OwnsAsync(Guid productId, string url, CancellationToken cancellationToken);
}
