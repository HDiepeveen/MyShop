using MyShop.Domain.Catalog;

namespace MyShop.Api.Tests.Catalog.Products;

public sealed class VariantAttributeHttpFlowTests
{
    [Theory]
    [InlineData(AttributeDataType.Text, "\" Red \"")]
    [InlineData(AttributeDataType.Integer, "9223372036854775807")]
    [InlineData(AttributeDataType.Decimal, "1.2345678901234567890123456789")]
    [InlineData(AttributeDataType.Boolean, "false")]
    [InlineData(AttributeDataType.Date, "\"2026-01-01\"")]
    [InlineData(AttributeDataType.Choice, "\"Red\"")]
    [InlineData(AttributeDataType.MultiChoice, "[\"Red\",\"Blue\"]")]
    public async Task BoundEndpoints_RoundTripAllTypesAndKeepRevisionForEqualValues(AttributeDataType type, string jsonValue)
    {
        await using var scenario = new AttributeHttpScenario(type, AttributeScope.Variant);
        var repository = scenario.Repository;
        var initialRevision = repository.Token.Revision;
        var body = "{\"dataType\":" + (int)type + ",\"value\":" + jsonValue + "}";

        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", body)).Response.StatusCode);
        var original = Assert.Single(repository.Variant.AttributeValues);
        var revision = repository.Token.Revision;
        Assert.NotEqual(initialRevision, revision);
        Assert.Equal(1, repository.SaveCalls);
        var get = await scenario.Send("GetVariantAttributeValue");
        Assert.Equal(200, get.Response.StatusCode);
        using var json = await AttributeHttpScenario.Read(get);
        var attribute = json.RootElement.GetProperty("attribute");
        Assert.Equal(repository.DefinitionId.Value, attribute.GetProperty("attributeDefinitionId").GetGuid());
        Assert.Equal(type.ToString(), attribute.GetProperty("dataType").GetString());
        Assert.Equal(jsonValue, attribute.GetProperty("value").GetRawText());
        Assert.Equal(revision, json.RootElement.GetProperty("revision").GetGuid());

        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", body)).Response.StatusCode);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Equal(revision, repository.Token.Revision);
        Assert.Same(original, Assert.Single(repository.Variant.AttributeValues));
        Assert.Equal(404, (await scenario.Send("GetProductAttributeValue")).Response.StatusCode);
        Assert.Equal(1, repository.SaveCalls);

        Assert.Equal(204, (await scenario.Send("RemoveVariantAttributeValue")).Response.StatusCode);
        Assert.Equal(2, repository.SaveCalls);
        Assert.NotEqual(revision, repository.Token.Revision);
        Assert.Equal(404, (await scenario.Send("GetVariantAttributeValue")).Response.StatusCode);
        Assert.Equal(204, (await scenario.Send("RemoveVariantAttributeValue")).Response.StatusCode);
        Assert.Equal(2, repository.SaveCalls);
        Assert.Empty(repository.Variant.AttributeValues);
        Assert.Empty(repository.Product.AttributeValues);
        Assert.Empty(repository.Other.AttributeValues);
    }

    [Theory]
    [InlineData("{")]
    [InlineData("")]
    [InlineData("{\"dataType\":0,\"value\":null}")]
    public async Task BoundEndpoint_RejectsInvalidJsonWithoutChangingStoredValue(string body)
    {
        await using var scenario = new AttributeHttpScenario(AttributeDataType.Text, AttributeScope.Variant);
        var original = TextAttributeValue.Create(scenario.Repository.DefinitionId, "Original");
        scenario.Repository.Product.SetVariantAttributeValue(scenario.Repository.Variant.Id, original);
        Assert.Equal(400, (await scenario.Send("SetVariantAttributeValue", body)).Response.StatusCode);
        Assert.Equal(0, scenario.Repository.ReadCalls);
        Assert.Equal(0, scenario.Repository.SaveCalls);
        Assert.Same(original, Assert.Single(scenario.Repository.Variant.AttributeValues));
    }

    [Fact]
    public async Task BoundEndpoints_DoNotReadOrRemoveAnotherVariantsValue()
    {
        await using var scenario = new AttributeHttpScenario(AttributeDataType.Text, AttributeScope.Variant);
        var repository = scenario.Repository;
        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", "{\"dataType\":0,\"value\":\"Red\"}")).Response.StatusCode);
        var original = Assert.Single(repository.Variant.AttributeValues);
        Assert.Equal(404, (await scenario.Send("GetVariantAttributeValue", variantId: repository.Other.Id.Value)).Response.StatusCode);
        Assert.Equal(204, (await scenario.Send("RemoveVariantAttributeValue", variantId: repository.Other.Id.Value)).Response.StatusCode);
        Assert.Equal(1, repository.SaveCalls);
        Assert.Same(original, Assert.Single(repository.Variant.AttributeValues));
        Assert.Empty(repository.Other.AttributeValues);
    }

    [Fact]
    public async Task BoundEndpoint_ChangedValueAdvancesRevisionAndWrongTypeDoesNot()
    {
        await using var scenario = new AttributeHttpScenario(AttributeDataType.Text, AttributeScope.Variant);
        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", "{\"dataType\":0,\"value\":\"Red\"}")).Response.StatusCode);
        var revision = scenario.Repository.Token.Revision;
        Assert.Equal(204, (await scenario.Send("SetVariantAttributeValue", "{\"dataType\":0,\"value\":\"Blue\"}")).Response.StatusCode);
        Assert.NotEqual(revision, scenario.Repository.Token.Revision);
        revision = scenario.Repository.Token.Revision;
        Assert.Equal(409, (await scenario.Send("SetVariantAttributeValue", "{\"dataType\":1,\"value\":1}")).Response.StatusCode);
        Assert.Equal(revision, scenario.Repository.Token.Revision);
        Assert.Equal(2, scenario.Repository.SaveCalls);
        Assert.Equal("Blue", Assert.IsType<TextAttributeValue>(Assert.Single(scenario.Repository.Variant.AttributeValues)).Value);
    }
}
