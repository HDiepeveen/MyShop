using MyShop.Infrastructure.Persistence.Mappers;

namespace MyShop.Infrastructure.Tests.Persistence.Mappers;

public sealed class DecimalAttributeValuePersistenceConverterTests
{
    public static IEnumerable<object[]> ValidValues()
    {
        yield return [0m, 0m, (byte)0];
        yield return [new decimal(0, 0, 0, true, 28), 0m, (byte)0];
        yield return [1m, 1m, (byte)0];
        yield return [-1m, -1m, (byte)0];
        yield return [1.0m, 1m, (byte)0];
        yield return [1.2300m, 123m, (byte)2];
        yield return [-1.2300m, -123m, (byte)2];
        yield return [-987.654321m, -987654321m, (byte)6];
        yield return [new decimal(1, 0, 0, false, 28), 1m, (byte)28];
        yield return [decimal.MaxValue, decimal.MaxValue, (byte)0];
        yield return [decimal.MinValue, decimal.MinValue, (byte)0];
        yield return [new decimal(1, 1, 0, false, 5), new decimal(1, 1, 0, false, 0), (byte)5];
        yield return [new decimal(1, 1, 1, false, 5), new decimal(1, 1, 1, false, 0), (byte)5];
        yield return [new decimal(-1, -1, -1, false, 1), decimal.MaxValue, (byte)1];
        yield return [new decimal(-1, -1, -1, true, 1), decimal.MinValue, (byte)1];
    }

    [Theory]
    [MemberData(nameof(ValidValues))]
    public void ToPersistence_CanonicalizesAndRoundTripsExactly(
        decimal value,
        decimal expectedCoefficient,
        byte expectedScale)
    {
        var persisted = DecimalAttributeValuePersistenceConverter.ToPersistence(value);

        Assert.Equal(expectedCoefficient, persisted.Coefficient);
        Assert.Equal(expectedScale, persisted.Scale);
        Assert.Equal(0, GetScale(persisted.Coefficient));
        Assert.Equal(value, DecimalAttributeValuePersistenceConverter.ToDomain(
            persisted.Coefficient, persisted.Scale));
    }

    public static IEnumerable<object[]> MalformedValues()
    {
        yield return [123.0m, (byte)2];
        yield return [123.00m, (byte)2];
        yield return [-123.0m, (byte)2];
        yield return [1.5m, (byte)0];
        yield return [1m, (byte)29];
        yield return [0m, (byte)1];
        yield return [10m, (byte)1];
        yield return [-10m, (byte)1];
    }

    [Theory]
    [MemberData(nameof(MalformedValues))]
    public void ToDomain_RejectsMalformedOrNoncanonicalPersistence(decimal coefficient, byte scale)
    {
        Assert.Throws<InvalidOperationException>(() =>
            DecimalAttributeValuePersistenceConverter.ToDomain(coefficient, scale));
    }

    private static byte GetScale(decimal value) =>
        (byte)((decimal.GetBits(value)[3] >> 16) & 0xff);
}
