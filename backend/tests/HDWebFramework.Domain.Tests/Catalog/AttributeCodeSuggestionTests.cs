using HDWebFramework.Domain.Catalog;
namespace HDWebFramework.Domain.Tests.Catalog;
public class AttributeCodeSuggestionTests
{
    [Theory]
    [InlineData("Kenteken", "kenteken")]
    [InlineData("Transmissie type", "transmissie_type")]
    [InlineData("Café", "cafe")]
    [InlineData("123 nummer", "kenmerk_123_nummer")]
    public void Suggest_produces_valid_codes(string name, string expected)
    {
        Assert.Equal(expected, AttributeCode.Suggest(name));
        Assert.Equal(expected, AttributeCode.Create(AttributeCode.Suggest(name)).Value);
    }
}
