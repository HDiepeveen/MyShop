using System.Buffers.Binary;
using MyShop.Application.Catalog.Abstractions;

namespace MyShop.Application.Tests;

public sealed class ProductImageUploadTests
{
    private static byte[] Png() => Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+jZxkAAAAASUVORK5CYII=");

    [Fact]
    public void Accepts_a_real_jpeg_and_rejects_a_frame_without_image_data()
    {
        var jpeg = Convert.FromBase64String("/9j/4AAQSkZJRgABAQEAYABgAAD/2wBDAAMCAgMCAgMDAwMEAwMEBQgFBQQEBQoHBwYIDAoMDAsKCwsNDhIQDQ4RDgsLEBYQERMUFRUVDA8XGBYUGBIUFRT/2wBDAQMEBAUEBQkFBQkUDQsNFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBT/wAARCAABAAEDASIAAhEBAxEB/8QAHwAAAQUBAQEBAQEAAAAAAAAAAAECAwQFBgcICQoL/8QAtRAAAgEDAwIEAwUFBAQAAAF9AQIDAAQRBRIhMUEGE1FhByJxFDKBkaEII0KxwRVS0fAkM2JyggkKFhcYGRolJicoKSo0NTY3ODk6Q0RFRkdISUpTVFVWV1hZWmNkZWZnaGlqc3R1dnd4eXqDhIWGh4iJipKTlJWWl5iZmqKjpKWmp6ipqrKztLW2t7i5usLDxMXGx8jJytLT1NXW19jZ2uHi4+Tl5ufo6erx8vP09fb3+Pn6/8QAHwEAAwEBAQEBAQEBAQAAAAAAAAECAwQFBgcICQoL/8QAtREAAgECBAQDBAcFBAQAAQJ3AAECAxEEBSExBhJBUQdhcRMiMoEIFEKRobHBCSMzUvAVYnLRChYkNOEl8RcYGRomJygpKjU2Nzg5OkNERUZHSElKU1RVVldYWVpjZGVmZ2hpanN0dXZ3eHl6goOEhYaHiImKkpOUlZaXmJmaoqOkpaanqKmqsrO0tba3uLm6wsPExcbHyMnK0tPU1dbX2Nna4uPk5ebn6Onq8vP09fb3+Pn6/9oADAMBAAIRAxEAPwD8qqKKKAP/2Q==");
        Assert.Equal("image/jpeg", ProductImageUpload.Create(jpeg, "Shirt", "shirt.jpg").ContentType);
        byte[] frameOnly = [255, 216, 255, 192, 0, 8, 8, 0, 1, 0, 1, 1, 255, 217];
        Assert.Throws<ArgumentException>(() => ProductImageUpload.Create(frameOnly, "Shirt", "shirt.jpg"));
    }

    [Fact]
    public void Detects_content_independently_of_file_name_and_trims_metadata()
    {
        var image = ProductImageUpload.Create(Png(), " Front of shirt ", "C:\\fakepath\\shirt.png");
        Assert.Equal("image/png", image.ContentType); Assert.Equal("shirt.png", image.FileName);
        Assert.Equal("Front of shirt", image.AlternativeText);
    }

    [Fact]
    public void Rejects_executable_text_svg_truncated_images_and_excessive_dimensions()
    {
        foreach (var bytes in new[] { "<svg></svg>"u8.ToArray(), "<script>alert(1)</script>"u8.ToArray(), Png()[..40], Array.Empty<byte>(), new byte[ProductImageUpload.MaximumBytes + 1] })
            Assert.Throws<ArgumentException>(() => ProductImageUpload.Create(bytes, "Shirt", "shirt.png"));
        var huge = Png(); BinaryPrimitives.WriteUInt32BigEndian(huge.AsSpan(16, 4), 100_000);
        BinaryPrimitives.WriteUInt32BigEndian(huge.AsSpan(20, 4), 100_000);
        Assert.Throws<ArgumentException>(() => ProductImageUpload.Create(huge, "Shirt", "shirt.png"));
    }

    [Theory]
    [InlineData("", "shirt.png")]
    [InlineData("Shirt", "")]
    [InlineData("Shirt", "bad\nname.png")]
    public void Requires_safe_metadata(string alt, string fileName) =>
        Assert.Throws<ArgumentException>(() => ProductImageUpload.Create(Png(), alt, fileName));
}
