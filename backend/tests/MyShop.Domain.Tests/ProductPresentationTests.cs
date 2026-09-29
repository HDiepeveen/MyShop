using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductPresentationTests
{
    [Fact]
    public void NewProductsStartAsEmptyDrafts()
    {
        var product = Product.Create("Shirt", ProductTypeId.New(), "Default");
        Assert.Equal(ProductPresentation.Draft, product.Presentation);
    }
    [Fact]
    public void NormalizesAndPublishesCompletePresentation()
    {
        var presentation = ProductPresentation.Create(" Description ", " https://example.com/shirt.jpg ", " Linen shirt ", true);
        Assert.Equal("Description", presentation.Description);
        Assert.Equal("https://example.com/shirt.jpg", presentation.ImageUrl);
        Assert.Equal("Linen shirt", presentation.ImageAlt);
        Assert.True(presentation.IsPublished);
    }
    [Theory]
    [InlineData("http://example.com/a.jpg")]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:image/png;base64,abc")]
    [InlineData("//example.com/a.jpg")]
    [InlineData("https://user:password@example.com/a.jpg")]
    [InlineData("https://example.com/\\a")]
    [InlineData("https://example.com/a\nb")]
    public void RejectsUnsafeImageLinks(string url) =>
        Assert.Throws<ArgumentException>(() => ProductPresentation.Create("Description", url, "Image", false));
    [Theory]
    [InlineData("", null, "", true)]
    [InlineData("Description", null, "", true)]
    [InlineData("", "https://example.com/a.jpg", "Image", true)]
    [InlineData("Description", "https://example.com/a.jpg", "", false)]
    [InlineData("Description", null, "Image", false)]
    public void RejectsIncompletePublicationOrImage(string description, string? url, string alt, bool published) =>
        Assert.Throws<ArgumentException>(() => ProductPresentation.Create(description, url, alt, published));
    [Fact]
    public void LimitsStoredTextAndUrlLengths()
    {
        Assert.Throws<ArgumentException>(() => ProductPresentation.Create(new string('a', 10001), null, "", false));
        Assert.Throws<ArgumentException>(() => ProductPresentation.Create("", "https://example.com/" + new string('a', 2048), "Image", false));
        Assert.Throws<ArgumentException>(() => ProductPresentation.Create("", "https://example.com/a", new string('a', 251), false));
        Assert.Equal(10000, ProductPresentation.Create(new string('a', 10000), null, "", false).Description.Length);
    }
    [Fact]
    public void RemovingPresentationRequiresExplicitDraftAndPreservesOtherProductData()
    {
        var product = Product.Create("Shirt", ProductTypeId.New(), "Default");
        product.SetPresentation(ProductPresentation.Create("Description", "https://example.com/a.jpg", "Image", true));
        product.SetPresentation(ProductPresentation.Draft);
        Assert.False(product.Presentation.IsPublished);
        Assert.Equal("Shirt", product.Name);
        Assert.Single(product.Variants);
        Assert.Throws<ArgumentNullException>(() => product.SetPresentation(null!));
        Assert.Equal(ProductPresentation.Draft, product.Presentation);
    }
}
