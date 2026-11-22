using HDWebFramework.Application.Catalog.ImportOverheidIo;
using HDWebFramework.Application.Catalog.Abstractions;
using HDWebFramework.Application.Catalog.Rdw;
using HDWebFramework.Application.Catalog.OverheidIo;
using HDWebFramework.Domain.Catalog;

namespace HDWebFramework.Application.Tests.Catalog;

public sealed class OverheidIoTests
{
    [Theory]
    [InlineData(" ab-12-cd ", "AB12CD")]
    [InlineData("12 abc 3", "12ABC3")]
    public void NormalizesPlates(string value, string expected) =>
        Assert.Equal(expected, OverheidIoAttributeValue.NormalizePlate(value));

    [Theory]
    [InlineData("")]
    [InlineData("ABCDEF")]
    [InlineData("123456")]
    [InlineData("AB12C' OR 1=1")]
    public void RejectsInvalidPlates(string value) =>
        Assert.Throws<ArgumentException>(() => OverheidIoAttributeValue.NormalizePlate(value));

    [Fact]
    public void DisplaysOnlySelectedExistingFieldsAndFormatsDates()
    {
        var fields = RdwDisplay.Fields("""{"kenteken":"AB12CD","merk":"OPEL","vervaldatum_apk":"20271118","eerste_kleur":""}""",
            ["merk", "vervaldatum_apk", "eerste_kleur", "cilinderinhoud"]);
        Assert.Equal(new[] { "merk", "vervaldatum_apk" }, fields.Select(f => f.Key));
        Assert.Equal("18-11-2027", fields[1].Value);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ImportsOnlyWhenRdwFindsVehicle(bool found)
    {
        var type = ProductType.Create("Auto");
        var id = AttributeDefinitionId.New();
        type.AddAttribute(id, AttributeCode.Create("rdw"), "Kenteken", AttributeDataType.OverheidIo, false, false, AttributeScope.Product);
        var product = Product.Create("Auto", type.Id);
        var repo = new ProductStore(product);
        var client = new Client(found);
        var useCase = new ImportOverheidIo(repo, new TypeStore(type), client);
        Assert.Equal(found, await useCase.ExecuteAsync(product.Id.Value, id.Value, "ab-12-cd", default));
        Assert.Equal("AB-12-CD", client.Request);
        Assert.Equal(found ? 1 : 0, repo.Saves);
        if (found) Assert.Equal("AB12CD", Assert.IsType<OverheidIoAttributeValue>(Assert.Single(product.AttributeValues)).LicensePlate);
    }

    [Fact]
    public async Task RejectsNonRdwDefinitionBeforeCallingRdw()
    {
        var type = ProductType.Create("Auto");
        var id = AttributeDefinitionId.New();
        type.AddAttribute(id, AttributeCode.Create("text"), "Tekst", AttributeDataType.Text, false, false, AttributeScope.Product);
        var product = Product.Create("Auto", type.Id);
        var client = new Client(true);
        await Assert.ThrowsAsync<ArgumentException>(() => new ImportOverheidIo(new ProductStore(product), new TypeStore(type), client)
            .ExecuteAsync(product.Id.Value, id.Value, "AB12CD", default));
        Assert.Null(client.Request);
    }

    private sealed class Client(bool found) : IOverheidIoClient
    {
        public string? Request { get; private set; }
        public Task<string?> FindAsync(string plate, CancellationToken ct)
        {
            Request = plate;
            return Task.FromResult(found ? """{"kenteken":"AB12CD","merk":"OPEL"}""" : null);
        }
    }
    private sealed class TypeStore(ProductType type) : IProductTypeRepository
    {
        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken ct) => Task.FromResult<ProductType?>(type);
    }
    private sealed class ProductStore(Product product) : IProductRepository
    {
        private readonly ProductConcurrencyToken revision = ProductConcurrencyToken.Create(product.Id, Guid.NewGuid());
        public int Saves { get; private set; }
        public Task<ProductSnapshot?> GetByIdAsync(ProductId id, CancellationToken ct) => Task.FromResult<ProductSnapshot?>(new(product, revision));
        public Task<ProductConcurrencyToken> AddAsync(Product product, CancellationToken ct) => throw new NotSupportedException();
        public Task<ProductConcurrencyToken> SaveAsync(Product value, ProductConcurrencyToken expected, CancellationToken ct)
        {
            Assert.Same(revision, expected);
            Saves++;
            return Task.FromResult(revision);
        }
    }
}
