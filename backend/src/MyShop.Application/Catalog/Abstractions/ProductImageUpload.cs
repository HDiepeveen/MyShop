using System.Buffers.Binary;

namespace MyShop.Application.Catalog.Abstractions;

public sealed record ProductImageUpload
{
    public const int MaximumBytes = 5 * 1024 * 1024;
    public const int MaximumImages = 10;
    private ProductImageUpload(byte[] bytes, string contentType, string alternativeText, string fileName)
    { Bytes = bytes; ContentType = contentType; AlternativeText = alternativeText; FileName = fileName; }
    public byte[] Bytes { get; }
    public string ContentType { get; }
    public string AlternativeText { get; }
    public string FileName { get; }

    public static ProductImageUpload Create(byte[] bytes, string alternativeText, string fileName)
    {
        ArgumentNullException.ThrowIfNull(bytes);
        ArgumentNullException.ThrowIfNull(alternativeText);
        ArgumentNullException.ThrowIfNull(fileName);
        alternativeText = alternativeText.Trim();
        fileName = fileName.Replace('\\', '/').Split('/').Last().Trim();
        if (bytes.Length == 0 || bytes.Length > MaximumBytes || alternativeText.Length is < 1 or > 250 ||
            fileName.Length is < 1 or > 120 || fileName.Any(char.IsControl))
            throw new ArgumentException("Use JPEG or PNG, at most 5 MB, with alternative text and a file name.");
        var contentType = Detect(bytes);
        return new(bytes, contentType, alternativeText, fileName);
    }

    private static string Detect(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 45 && bytes[..8].SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }) &&
            BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(8, 4)) == 13 && bytes.Slice(12, 4).SequenceEqual("IHDR"u8))
        {
            CheckDimensions(BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(16, 4)), BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(20, 4)));
            var offset = 8;
            var hasData = false;
            while (offset <= bytes.Length - 12)
            {
                var length = BinaryPrimitives.ReadUInt32BigEndian(bytes.Slice(offset, 4));
                if (length > bytes.Length - offset - 12) break;
                var type = bytes.Slice(offset + 4, 4);
                if (type.SequenceEqual("IDAT"u8)) hasData = true;
                if (type.SequenceEqual("IEND"u8) && length == 0 && hasData && offset + 12 == bytes.Length) return "image/png";
                offset += (int)length + 12;
            }
        }
        else if (bytes.Length >= 4 && bytes[0] == 255 && bytes[1] == 216 && bytes[^2] == 255 && bytes[^1] == 217)
        {
            var offset = 2;
            var hasFrame = false;
            while (offset < bytes.Length - 2)
            {
                if (bytes[offset++] != 255) break;
                while (offset < bytes.Length && bytes[offset] == 255) offset++;
                if (offset >= bytes.Length) break;
                var marker = bytes[offset++];
                if (offset > bytes.Length - 2) break;
                var length = BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset, 2));
                if (length < 2 || length > bytes.Length - offset) break;
                if (marker is 192 or 193 or 194 && length >= 8)
                {
                    CheckDimensions(BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 5, 2)), BinaryPrimitives.ReadUInt16BigEndian(bytes.Slice(offset + 3, 2)));
                    hasFrame = true;
                }
                if (marker == 218)
                {
                    if (hasFrame && length >= 6 && offset + length < bytes.Length - 2) return "image/jpeg";
                    break;
                }
                offset += length;
            }
        }
        throw new ArgumentException("Only complete JPEG and PNG images are supported.");
    }

    private static void CheckDimensions(uint width, uint height)
    {
        if (width == 0 || height == 0 || (ulong)width * height > 20_000_000)
            throw new ArgumentException("Images must not exceed 20 million pixels.");
    }
}
