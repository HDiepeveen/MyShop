using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Catalog.ManageProductImages;

public sealed class ManageProductImages(IProductImages images)
{
    public Task<ProductImages?> ExecuteAsync(Guid productId, CancellationToken ct) => images.GetAsync(productId, ct);
    public Task<ProductImageContent?> ContentAsync(Guid imageId, bool administrator, CancellationToken ct) => images.ContentAsync(imageId, administrator, ct);
    public Task<bool> UploadAsync(Guid productId, Guid revision, IReadOnlyList<ProductImageUpload> uploads, CancellationToken ct) => images.UploadAsync(productId, revision, uploads, ct);
    public Task<bool> SetMainAsync(Guid productId, Guid imageId, Guid revision, CancellationToken ct) => images.SetMainAsync(productId, imageId, revision, ct);
    public Task<bool> DeleteAsync(Guid productId, Guid imageId, Guid revision, CancellationToken ct) => images.DeleteAsync(productId, imageId, revision, ct);
    public Task<bool> OwnsAsync(Guid productId, string url, CancellationToken ct) => images.OwnsAsync(productId, url, ct);
}
