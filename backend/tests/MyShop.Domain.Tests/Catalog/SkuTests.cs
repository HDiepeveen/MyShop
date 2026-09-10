using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class SkuTests
{
    [Fact]
    public void Create_WithValidValue_PreservesCanonicalValue()
    {
        var sku = Sku.Create("ABC-123");

        Assert.Equal("ABC-123", sku.Value);
    }

    [Fact]
    public void Create_LowercaseValue_CanonicalizesToUppercase()
    {
        var sku = Sku.Create("abc-123");

        Assert.Equal("ABC-123", sku.Value);
    }

    [Fact]
    public void Create_DifferentlyCasedValues_AreEqual()
    {
        var lowercase = Sku.Create("abc-123");
        var uppercase = Sku.Create("ABC-123");

        Assert.Equal(lowercase, uppercase);
    }

    [Fact]
    public void Create_WithNullValue_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => Sku.Create(null!));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public void Create_WithEmptyOrWhitespaceOnlyValue_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Sku.Create(value));
    }

    [Theory]
    [InlineData(" ABC")]
    [InlineData("ABC ")]
    [InlineData("AB C")]
    public void Create_WithWhitespace_Throws(string value)
    {
        Assert.Throws<ArgumentException>(() => Sku.Create(value));
    }

    [Fact]
    public void Create_WithMaximumLengthValue_IsAccepted()
    {
        var value = new string('a', Sku.MaximumLength);

        var sku = Sku.Create(value);

        Assert.Equal(value.ToUpperInvariant(), sku.Value);
    }

    [Fact]
    public void Create_WithValueLongerThanMaximumLength_Throws()
    {
        var value = new string('a', Sku.MaximumLength + 1);

        Assert.Throws<ArgumentException>(() => Sku.Create(value));
    }

    [Fact]
    public void Create_PreservesLeadingZeroes()
    {
        var sku = Sku.Create("00123");

        Assert.Equal("00123", sku.Value);
    }

    [Theory]
    [InlineData("ABC-123")]
    [InlineData("ABC_123")]
    [InlineData("ABC/123")]
    [InlineData("ABC.123")]
    public void Create_AllowsNonWhitespacePunctuation(string value)
    {
        var sku = Sku.Create(value);

        Assert.Equal(value, sku.Value);
    }

    [Fact]
    public void ToString_ReturnsCanonicalValue()
    {
        var sku = Sku.Create("abc-123");

        Assert.Equal("ABC-123", sku.ToString());
    }
}