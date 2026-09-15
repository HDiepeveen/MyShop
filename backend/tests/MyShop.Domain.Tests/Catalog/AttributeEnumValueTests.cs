using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class AttributeEnumValueTests
{
    [Fact]
    public void AttributeDataType_UsesStablePersistedValues()
    {
        Assert.Equal(0, (int)AttributeDataType.Text);
        Assert.Equal(1, (int)AttributeDataType.Integer);
        Assert.Equal(2, (int)AttributeDataType.Decimal);
        Assert.Equal(3, (int)AttributeDataType.Boolean);
        Assert.Equal(4, (int)AttributeDataType.Date);
        Assert.Equal(5, (int)AttributeDataType.Choice);
        Assert.Equal(6, (int)AttributeDataType.MultiChoice);
    }

    [Fact]
    public void AttributeScope_UsesStablePersistedValues()
    {
        Assert.Equal(0, (int)AttributeScope.Product);
        Assert.Equal(1, (int)AttributeScope.Variant);
    }
}