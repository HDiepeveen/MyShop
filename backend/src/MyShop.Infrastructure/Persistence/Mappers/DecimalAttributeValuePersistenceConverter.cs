using System.Numerics;

namespace MyShop.Infrastructure.Persistence.Mappers;

internal static class DecimalAttributeValuePersistenceConverter
{
    private static readonly BigInteger MaximumCoefficient = (BigInteger.One << 96) - BigInteger.One;

    internal static (decimal Coefficient, byte Scale) ToPersistence(decimal value)
    {
        var bits = decimal.GetBits(value);
        var magnitude = GetMagnitude(bits);
        if (magnitude.IsZero)
            return (0m, 0);

        var scale = (byte)((bits[3] >> 16) & 0xff);
        while (scale > 0)
        {
            var quotient = BigInteger.DivRem(magnitude, 10, out var remainder);
            if (!remainder.IsZero)
                break;

            magnitude = quotient;
            scale--;
        }

        return (CreateDecimal(magnitude, bits[3] < 0, 0), scale);
    }

    internal static decimal ToDomain(decimal coefficient, byte scale)
    {
        if (scale > 28)
            throw new InvalidOperationException($"Persisted decimal scale '{scale}' must be between 0 and 28.");

        var bits = decimal.GetBits(coefficient);
        var coefficientScale = (byte)((bits[3] >> 16) & 0xff);
        if (coefficientScale != 0)
            throw new InvalidOperationException("Persisted decimal coefficient must have CLR scale zero.");

        var magnitude = GetMagnitude(bits);
        if (magnitude.IsZero)
        {
            if (scale != 0)
                throw new InvalidOperationException("Persisted decimal zero must use scale zero.");

            return 0m;
        }

        if (scale > 0 && magnitude % 10 == 0)
            throw new InvalidOperationException("Persisted decimal coefficient and scale are not canonical.");
        if (magnitude > MaximumCoefficient)
            throw new InvalidOperationException("Persisted decimal coefficient exceeds the System.Decimal range.");

        return CreateDecimal(magnitude, bits[3] < 0, scale);
    }

    private static BigInteger GetMagnitude(int[] bits) =>
        ((BigInteger)(uint)bits[2] << 64) |
        ((BigInteger)(uint)bits[1] << 32) |
        (uint)bits[0];

    private static decimal CreateDecimal(BigInteger magnitude, bool isNegative, byte scale)
    {
        var low = (uint)(magnitude & uint.MaxValue);
        var middle = (uint)((magnitude >> 32) & uint.MaxValue);
        var high = (uint)((magnitude >> 64) & uint.MaxValue);
        return new decimal(unchecked((int)low), unchecked((int)middle), unchecked((int)high), isNegative, scale);
    }
}
