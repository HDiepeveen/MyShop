using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class AttributeValueTests
{
    [Fact]
    public void Text_Create_PreservesValueAndReportsDataType()
    {
        // Arrange
        var definitionId = AttributeDefinitionId.New();
        const string text = "  Robert C. Martin  ";

        // Act
        var value = TextAttributeValue.Create(definitionId, text);

        // Assert
        Assert.Equal(definitionId, value.AttributeDefinitionId);
        Assert.Equal(text, value.Value);
        Assert.Equal(AttributeDataType.Text, value.DataType);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Text_Create_WithInvalidText_Throws(string? text)
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => TextAttributeValue.Create(AttributeDefinitionId.New(), text!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Theory]
    [InlineData(-1L)]
    [InlineData(0L)]
    [InlineData(464L)]
    public void Integer_Create_AcceptsAnyLongAndReportsDataType(long number)
    {
        // Arrange
        var definitionId = AttributeDefinitionId.New();

        // Act
        var value = IntegerAttributeValue.Create(definitionId, number);

        // Assert
        Assert.Equal(number, value.Value);
        Assert.Equal(AttributeDataType.Integer, value.DataType);
    }

    [Fact]
    public void Decimal_Create_PreservesExactValueAndReportsDataType()
    {
        // Arrange
        const decimal number = 123.456789m;

        // Act
        var value = DecimalAttributeValue.Create(AttributeDefinitionId.New(), number);

        // Assert
        Assert.Equal(number, value.Value);
        Assert.Equal(AttributeDataType.Decimal, value.DataType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Boolean_Create_AcceptsBothValuesAndReportsDataType(bool boolean)
    {
        // Arrange

        // Act
        var value = BooleanAttributeValue.Create(AttributeDefinitionId.New(), boolean);

        // Assert
        Assert.Equal(boolean, value.Value);
        Assert.Equal(AttributeDataType.Boolean, value.DataType);
    }

    [Fact]
    public void Date_Create_PreservesDateOnlyAndReportsDataType()
    {
        // Arrange
        var date = new DateOnly(2025, 1, 31);

        // Act
        var value = DateAttributeValue.Create(AttributeDefinitionId.New(), date);

        // Assert
        Assert.Equal(date, value.Value);
        Assert.Equal(AttributeDataType.Date, value.DataType);
    }

    [Fact]
    public void Choice_Create_PreservesChoiceAndReportsDataType()
    {
        // Arrange
        var choice = ChoiceValue.Create("electric");

        // Act
        var value = ChoiceAttributeValue.Create(AttributeDefinitionId.New(), choice);

        // Assert
        Assert.Same(choice, value.Value);
        Assert.Equal(AttributeDataType.Choice, value.DataType);
    }

    [Fact]
    public void ChoiceAttribute_Create_WithNullChoice_Throws()
    {
        // Arrange
        ChoiceValue choice = null!;

        // Act
        var exception = Record.Exception(() => ChoiceAttributeValue.Create(AttributeDefinitionId.New(), choice));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ChoiceValue_Create_WithInvalidValue_Throws(string? text)
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => ChoiceValue.Create(text!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
    }

    [Fact]
    public void ChoiceValue_Create_PreservesTextAndUsesOrdinalValueEquality()
    {
        // Arrange
        const string text = " Electric ";

        // Act
        var first = ChoiceValue.Create(text);
        var second = ChoiceValue.Create(text);
        var differentCase = ChoiceValue.Create(" electric ");

        // Assert
        Assert.Equal(text, first.Value);
        Assert.Equal(text, first.ToString());
        Assert.Equal(first, second);
        Assert.NotEqual(first, differentCase);
    }

    [Fact]
    public void MultiChoice_Create_PreservesOrderAndReportsDataType()
    {
        // Arrange
        var first = ChoiceValue.Create("black");
        var second = ChoiceValue.Create("white");

        // Act
        var value = MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), [first, second]);

        // Assert
        Assert.Equal([first, second], value.Values);
        Assert.Equal(AttributeDataType.MultiChoice, value.DataType);
    }

    [Fact]
    public void MultiChoice_Create_DefensivelyCopiesSuppliedCollection()
    {
        // Arrange
        var selections = new List<ChoiceValue> { ChoiceValue.Create("black") };
        var value = MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), selections);

        // Act
        selections.Add(ChoiceValue.Create("white"));

        // Assert
        Assert.Single(value.Values);
        Assert.Equal("black", value.Values[0].Value);
    }

    [Fact]
    public void MultiChoice_ValuesCannotBeMutatedThroughExposedPublicApi()
    {
        // Arrange
        var value = MultiChoiceAttributeValue.Create(
            AttributeDefinitionId.New(),
            [ChoiceValue.Create("black")]);
        dynamic exposedValues = value.Values;

        // Act
        var exception = Record.Exception(() => exposedValues.Add(ChoiceValue.Create("white")));

        // Assert
        Assert.NotNull(exception);
        Assert.Single(value.Values);
    }

    [Fact]
    public void MultiChoice_Create_WithNullCollection_Throws()
    {
        // Arrange
        IEnumerable<ChoiceValue> selections = null!;

        // Act
        var exception = Record.Exception(() =>
            MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), selections));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void MultiChoice_Create_WithEmptyCollection_Throws()
    {
        // Arrange
        ChoiceValue[] selections = [];

        // Act
        var exception = Record.Exception(() =>
            MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), selections));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void MultiChoice_Create_WithNullEntry_Throws()
    {
        // Arrange
        ChoiceValue[] selections = [ChoiceValue.Create("black"), null!];

        // Act
        var exception = Record.Exception(() =>
            MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), selections));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void MultiChoice_Create_WithDuplicateSelection_Throws()
    {
        // Arrange
        ChoiceValue[] selections = [ChoiceValue.Create("black"), ChoiceValue.Create("black")];

        // Act
        var exception = Record.Exception(() =>
            MultiChoiceAttributeValue.Create(AttributeDefinitionId.New(), selections));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void MultiChoice_EquivalentOrderedValuesHaveStructuralEquality()
    {
        // Arrange
        var definitionId = AttributeDefinitionId.New();

        // Act
        var first = MultiChoiceAttributeValue.Create(
            definitionId,
            [ChoiceValue.Create("black"), ChoiceValue.Create("white")]);
        var second = MultiChoiceAttributeValue.Create(
            definitionId,
            [ChoiceValue.Create("black"), ChoiceValue.Create("white")]);

        // Assert
        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void MultiChoice_DifferentOrderingIsNotEqual()
    {
        // Arrange
        var definitionId = AttributeDefinitionId.New();

        // Act
        var first = MultiChoiceAttributeValue.Create(
            definitionId,
            [ChoiceValue.Create("black"), ChoiceValue.Create("white")]);
        var reversed = MultiChoiceAttributeValue.Create(
            definitionId,
            [ChoiceValue.Create("white"), ChoiceValue.Create("black")]);

        // Assert
        Assert.NotEqual(first, reversed);
    }

    [Fact]
    public void TypedValueFactories_WithDefaultDefinitionId_Throw()
    {
        // Arrange
        Action[] factories =
        [
            () => TextAttributeValue.Create(default, "text"),
            () => IntegerAttributeValue.Create(default, 1),
            () => DecimalAttributeValue.Create(default, 1m),
            () => BooleanAttributeValue.Create(default, false),
            () => DateAttributeValue.Create(default, new DateOnly(2025, 1, 1)),
            () => ChoiceAttributeValue.Create(default, ChoiceValue.Create("choice")),
            () => MultiChoiceAttributeValue.Create(default, [ChoiceValue.Create("choice")])
        ];

        // Act
        var exceptions = factories.Select(Record.Exception).ToArray();

        // Assert
        Assert.All(exceptions, exception => Assert.IsType<ArgumentException>(exception));
    }

    [Fact]
    public void EquivalentScalarValuesHaveValueEquality()
    {
        // Arrange
        var definitionId = AttributeDefinitionId.New();

        // Act
        var first = IntegerAttributeValue.Create(definitionId, 2025);
        var second = IntegerAttributeValue.Create(definitionId, 2025);

        // Assert
        Assert.Equal(first, second);
    }
}
