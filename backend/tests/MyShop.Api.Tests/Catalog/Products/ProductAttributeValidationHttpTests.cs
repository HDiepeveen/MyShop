using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class ProductAttributeValidationHttpTests
{
    [Theory]
    [InlineData(AttributeDataType.Text, "\"value\"")]
    [InlineData(AttributeDataType.Integer, "0")]
    [InlineData(AttributeDataType.Decimal, "0.0")]
    [InlineData(AttributeDataType.Boolean, "false")]
    [InlineData(AttributeDataType.Date, "\"2026-09-29\"")]
    [InlineData(AttributeDataType.Choice, "\"Blue\"")]
    [InlineData(AttributeDataType.MultiChoice, "[\"Blue\",\"Red\"]")]
    public async Task RequiredProductValueBecomesValidAfterSetAndMissingAfterRemoval(AttributeDataType type, string value)
    {
        await using var scenario = new AttributeHttpScenario(type, AttributeScope.Product);
        var repository = scenario.Repository;
        repository.ProductType.SetAttributeRequired(repository.DefinitionId, true);
        using (var before = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation")))
        {
            Assert.False(before.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Equal("MissingRequired", Assert.Single(before.RootElement.GetProperty("issues").EnumerateArray()).GetProperty("code").GetString());
        }
        Assert.Equal(0, repository.SaveCalls);
        var body = "{\"dataType\":" + (int)type + ",\"value\":" + value + "}";
        Assert.Equal(204, (await scenario.Send("SetProductAttributeValue", body)).Response.StatusCode);
        using (var valid = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation")))
        {
            Assert.True(valid.RootElement.GetProperty("isValid").GetBoolean());
            Assert.Empty(valid.RootElement.GetProperty("issues").EnumerateArray());
        }
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(204, (await scenario.Send("RemoveProductAttributeValue")).Response.StatusCode);
        using var after = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation"));
        Assert.False(after.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(2, repository.SaveCalls);
    }

    [Fact]
    public async Task EveryVariantMustHaveItsOwnRequiredValue()
    {
        await using var scenario = new AttributeHttpScenario(AttributeDataType.Text, AttributeScope.Variant);
        var repository = scenario.Repository;
        repository.ProductType.SetAttributeRequired(repository.DefinitionId, true);
        using (var before = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation")))
            Assert.Equal(2, before.RootElement.GetProperty("issues").GetArrayLength());
        const string body = "{\"dataType\":0,\"value\":\"value\"}";
        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", body)).Response.StatusCode);
        using (var partial = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation")))
        {
            var issue = Assert.Single(partial.RootElement.GetProperty("issues").EnumerateArray());
            Assert.Equal(repository.Other.Id.Value, issue.GetProperty("variantId").GetGuid());
        }
        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", body, repository.Other.Id.Value)).Response.StatusCode);
        using var complete = await AttributeHttpScenario.Read(await scenario.Send("GetProductAttributeValidation"));
        Assert.True(complete.RootElement.GetProperty("isValid").GetBoolean());
        Assert.Equal(2, repository.SaveCalls);
    }

    [Fact]
    public async Task RemovedDefinitionIsReportedWithoutRemovingExistingValue()
    {
        await using var scenario = new AttributeHttpScenario(AttributeDataType.Text, AttributeScope.Product);
        var repository = scenario.Repository;
        Assert.Equal(204, (await scenario.Send("SetProductAttributeValue", "{\"dataType\":0,\"value\":\"retained\"}")).Response.StatusCode);
        repository.ProductType.RemoveAttribute(repository.DefinitionId);
        var response = await scenario.Send("GetProductAttributeValidation");
        Assert.Equal(200, response.Response.StatusCode);
        using var json = await AttributeHttpScenario.Read(response);
        var issue = Assert.Single(json.RootElement.GetProperty("issues").EnumerateArray());
        Assert.Equal("UnknownDefinition", issue.GetProperty("code").GetString());
        Assert.Equal(repository.DefinitionId.Value, issue.GetProperty("attributeDefinitionId").GetGuid());
        Assert.Equal(System.Text.Json.JsonValueKind.Null, issue.GetProperty("variantId").ValueKind);
        Assert.Single(repository.Product.AttributeValues);
        Assert.Equal(1, repository.SaveCalls);
    }
}
