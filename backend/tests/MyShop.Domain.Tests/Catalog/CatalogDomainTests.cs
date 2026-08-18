using MyShop.Domain.Catalog;

namespace MyShop.Domain.Tests.Catalog;

public sealed class ProductTypeTests
{
    [Fact]
    public void Create_WithValidName_GeneratesIdAndStartsWithoutAttributes()
    {
        // Arrange
        const string name = "Car";

        // Act
        var productType = ProductType.Create(name);

        // Assert
        Assert.NotEqual(default, productType.Id);
        Assert.NotEqual(Guid.Empty, productType.Id.Value);
        Assert.Equal(name, productType.Name);
        Assert.Empty(productType.AttributeDefinitions);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("\t")]
    public void Create_WithEmptyOrWhitespaceName_Throws(string name)
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => ProductType.Create(name));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Create_WithNullName_Throws()
    {
        // Arrange
        string name = null!;

        // Act
        var exception = Record.Exception(() => ProductType.Create(name));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Rename_ChangesNameWithoutChangingIdentity()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = productType.Id;

        // Act
        productType.Rename("Automobile");

        // Assert
        Assert.Equal("Automobile", productType.Name);
        Assert.Equal(id, productType.Id);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Rename_WithInvalidName_Throws(string? name)
    {
        // Arrange
        var productType = ProductType.Create("Car");

        // Act
        var exception = Record.Exception(() => productType.Rename(name!));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Equal("Car", productType.Name);
    }

    [Fact]
    public void AddAttribute_WithValidValues_AddsDefinition()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        var code = AttributeCode.Create("fuel_type");

        // Act
        var attribute = productType.AddAttribute(id, code, "Fuel type", AttributeDataType.Choice, true, true, AttributeScope.Variant);

        // Assert
        Assert.Same(attribute, Assert.Single(productType.AttributeDefinitions));
        Assert.Equal(id, attribute.Id);
        Assert.Equal(code, attribute.Code);
        Assert.Equal("Fuel type", attribute.DisplayName);
        Assert.Equal(AttributeDataType.Choice, attribute.DataType);
        Assert.True(attribute.IsRequired);
        Assert.True(attribute.IsFilterable);
        Assert.Equal(AttributeScope.Variant, attribute.Scope);
    }

    [Fact]
    public void AddAttribute_WithDuplicateCode_ThrowsAndDoesNotAdd()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var code = AttributeCode.Create("make");
        AddAttribute(productType, AttributeDefinitionId.New(), code);

        // Act
        var exception = Record.Exception(() => AddAttribute(productType, AttributeDefinitionId.New(), code));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Single(productType.AttributeDefinitions);
    }

    [Fact]
    public void AddAttribute_WithDuplicateId_ThrowsAndDoesNotAdd()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        AddAttribute(productType, id, AttributeCode.Create("make"));

        // Act
        var exception = Record.Exception(() => AddAttribute(productType, id, AttributeCode.Create("model")));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
        Assert.Single(productType.AttributeDefinitions);
    }

    [Fact]
    public void AddAttribute_WithDefaultId_Throws()
    {
        // Arrange
        var productType = ProductType.Create("Car");

        // Act
        var exception = Record.Exception(() => AddAttribute(productType, default, AttributeCode.Create("make")));

        // Assert
        Assert.IsType<ArgumentException>(exception);
        Assert.Empty(productType.AttributeDefinitions);
    }

    [Fact]
    public void AddAttribute_WithNullCode_Throws()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        AttributeCode code = null!;

        // Act
        var exception = Record.Exception(() => AddAttribute(productType, AttributeDefinitionId.New(), code));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
        Assert.Empty(productType.AttributeDefinitions);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void AddAttribute_WithInvalidInitialDisplayName_Throws(string? displayName)
    {
        // Arrange
        var productType = ProductType.Create("Car");

        // Act
        var exception = Record.Exception(() => productType.AddAttribute(
            AttributeDefinitionId.New(), AttributeCode.Create("make"), displayName!,
            AttributeDataType.Text, false, false, AttributeScope.Product));

        // Assert
        Assert.IsAssignableFrom<ArgumentException>(exception);
        Assert.Empty(productType.AttributeDefinitions);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void AddAttribute_WithUndefinedEnumValue_Throws(bool useInvalidDataType)
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var dataType = useInvalidDataType ? (AttributeDataType)999 : AttributeDataType.Text;
        var scope = useInvalidDataType ? AttributeScope.Product : (AttributeScope)999;

        // Act
        var exception = Record.Exception(() => productType.AddAttribute(
            AttributeDefinitionId.New(), AttributeCode.Create("make"), "Make", dataType, false, false, scope));

        // Assert
        Assert.IsType<ArgumentOutOfRangeException>(exception);
        Assert.Empty(productType.AttributeDefinitions);
    }

    [Fact]
    public void SameAttributeCode_CanBelongToDifferentProductTypes()
    {
        // Arrange
        var car = ProductType.Create("Car");
        var book = ProductType.Create("Book");
        var code = AttributeCode.Create("model");

        // Act
        AddAttribute(car, AttributeDefinitionId.New(), code);
        AddAttribute(book, AttributeDefinitionId.New(), code);

        // Assert
        Assert.Single(car.AttributeDefinitions);
        Assert.Single(book.AttributeDefinitions);
    }

    [Fact]
    public void RenameAttribute_ChangesDisplayNameButNotTechnicalIdentity()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        var attribute = AddAttribute(productType, id, AttributeCode.Create("make"));

        // Act
        productType.RenameAttribute(id, "Manufacturer");

        // Assert
        Assert.Equal("Manufacturer", attribute.DisplayName);
        Assert.Equal("make", attribute.Code.Value);
        Assert.Equal(id, attribute.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void RenameAttribute_WithInvalidDisplayName_Throws(string displayName)
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        AddAttribute(productType, id, AttributeCode.Create("make"));

        // Act
        var exception = Record.Exception(() => productType.RenameAttribute(id, displayName));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void SetAttributeRequired_CanChangeFromFalseToTrueAndBackToFalse()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        var attribute = AddAttribute(productType, id, AttributeCode.Create("make"));

        // Act
        productType.SetAttributeRequired(id, true);

        // Assert
        Assert.True(attribute.IsRequired);

        // Act
        productType.SetAttributeRequired(id, false);

        // Assert
        Assert.False(attribute.IsRequired);
    }

    [Fact]
    public void SetAttributeFilterable_CanChangeFromFalseToTrueAndBackToFalse()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var id = AttributeDefinitionId.New();
        var attribute = AddAttribute(productType, id, AttributeCode.Create("make"));

        // Act
        productType.SetAttributeFilterable(id, true);

        // Assert
        Assert.True(attribute.IsFilterable);

        // Act
        productType.SetAttributeFilterable(id, false);

        // Assert
        Assert.False(attribute.IsFilterable);
    }

    [Theory]
    [InlineData("rename")]
    [InlineData("required")]
    [InlineData("filterable")]
    [InlineData("remove")]
    public void AttributeOperation_WithUnknownId_Throws(string operation)
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var unknownId = AttributeDefinitionId.New();

        // Act
        var exception = Record.Exception(() => Perform(operation, productType, unknownId));

        // Assert
        Assert.IsType<InvalidOperationException>(exception);
    }

    [Fact]
    public void RemoveAttribute_RemovesDefinitionAndAllowsCodeReuse()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var originalId = AttributeDefinitionId.New();
        var code = AttributeCode.Create("make");
        AddAttribute(productType, originalId, code);

        // Act
        productType.RemoveAttribute(originalId);
        var replacement = AddAttribute(productType, AttributeDefinitionId.New(), code);

        // Assert
        Assert.Same(replacement, Assert.Single(productType.AttributeDefinitions));
        Assert.NotEqual(originalId, replacement.Id);
    }

    [Fact]
    public void AttributeDefinitions_CannotBeMutatedThroughExposedPublicApi()
    {
        // Arrange
        var productType = ProductType.Create("Car");
        var otherProductType = ProductType.Create("Book");
        var foreignAttribute = AddAttribute(
            otherProductType,
            AttributeDefinitionId.New(),
            AttributeCode.Create("author"));
        dynamic exposedAttributes = productType.AttributeDefinitions;

        // Act
        var exception = Record.Exception(() => exposedAttributes.Add(foreignAttribute));

        // Assert
        Assert.NotNull(exception);
        Assert.Empty(productType.AttributeDefinitions);
    }

    private static AttributeDefinition AddAttribute(ProductType productType, AttributeDefinitionId id, AttributeCode code) =>
        productType.AddAttribute(id, code, "Display name", AttributeDataType.Text, false, false, AttributeScope.Product);

    private static void Perform(string operation, ProductType productType, AttributeDefinitionId id)
    {
        switch (operation)
        {
            case "rename": productType.RenameAttribute(id, "Name"); break;
            case "required": productType.SetAttributeRequired(id, true); break;
            case "filterable": productType.SetAttributeFilterable(id, true); break;
            case "remove": productType.RemoveAttribute(id); break;
            default: throw new ArgumentOutOfRangeException(nameof(operation));
        }
    }
}

public sealed class AttributeCodeTests
{
    [Theory]
    [InlineData("make")]
    [InlineData("isbn")]
    [InlineData("fuel_type")]
    [InlineData("number_of_pages")]
    [InlineData("format2")]
    public void Create_WithValidValue_Succeeds(string value)
    {
        // Arrange

        // Act
        var code = AttributeCode.Create(value);

        // Assert
        Assert.Equal(value, code.Value);
        Assert.Equal(value, code.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("FuelType")]
    [InlineData("fuel type")]
    [InlineData("fuel-type")]
    [InlineData("2d_format")]
    [InlineData("_format")]
    [InlineData("cafe_\u00e9")]
    public void Create_WithInvalidValue_Throws(string value)
    {
        // Arrange

        // Act
        var exception = Record.Exception(() => AttributeCode.Create(value));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Create_WithNullValue_Throws()
    {
        // Arrange
        string value = null!;

        // Act
        var exception = Record.Exception(() => AttributeCode.Create(value));

        // Assert
        Assert.IsType<ArgumentNullException>(exception);
    }

    [Fact]
    public void Create_WithValueLongerThanMaximum_Throws()
    {
        // Arrange
        var value = new string('a', AttributeCode.MaximumLength + 1);

        // Act
        var exception = Record.Exception(() => AttributeCode.Create(value));

        // Assert
        Assert.IsType<ArgumentException>(exception);
    }

    [Fact]
    public void Codes_WithSameValue_HaveValueEquality()
    {
        // Arrange
        var first = AttributeCode.Create("fuel_type");
        var second = AttributeCode.Create("fuel_type");

        // Act
        var areEqual = first == second;

        // Assert
        Assert.True(areEqual);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }
}

public sealed class StronglyTypedIdTests
{
    [Fact]
    public void NewIds_AreNonEmptyAndDistinct()
    {
        // Arrange

        // Act
        var productTypeId = ProductTypeId.New();
        var otherProductTypeId = ProductTypeId.New();
        var attributeId = AttributeDefinitionId.New();
        var otherAttributeId = AttributeDefinitionId.New();

        // Assert
        Assert.NotEqual(Guid.Empty, productTypeId.Value);
        Assert.NotEqual(productTypeId, otherProductTypeId);
        Assert.NotEqual(Guid.Empty, attributeId.Value);
        Assert.NotEqual(attributeId, otherAttributeId);
    }
}
