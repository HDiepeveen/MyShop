using MyShop.Application.Catalog.Abstractions;
using MyShop.Application.Catalog.GetProductTypeAttribute;
using MyShop.Domain.Catalog;
using UseCase = MyShop.Application.Catalog.GetProductTypeAttribute.GetProductTypeAttribute;

namespace MyShop.Application.Tests.Catalog.GetProductTypeAttribute;

public sealed class GetProductTypeAttributeTests
{
    [Fact]
    public async Task ReturnsOnlyRequestedDefinitionAndForwardsToken()
    {
        var repository = new Repository();
        var attribute = repository.Type!.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("color"),
            "Color", AttributeDataType.Choice, true, true, AttributeScope.Variant);
        repository.Type.AddAttribute(AttributeDefinitionId.New(), AttributeCode.Create("size"),
            "Size", AttributeDataType.Text, false, false, AttributeScope.Product);
        using var source = new CancellationTokenSource();
        var result = await new UseCase(repository).ExecuteAsync(new(repository.Type.Id, attribute.Id), source.Token);
        Assert.True(result.IsSuccess);
        Assert.Null(result.Failure);
        Assert.Same(attribute, result.Attribute);
        Assert.Equal(repository.Type.Id, repository.RequestedId);
        Assert.Equal(source.Token, repository.Token);
        Assert.Equal(1, repository.Reads);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task DistinguishesMissingTypeAndDefinition(bool missingType)
    {
        var repository = new Repository();
        var id = repository.Type!.Id;
        if (missingType) repository.Type = null;
        var result = await new UseCase(repository).ExecuteAsync(new(id, AttributeDefinitionId.New()), CancellationToken.None);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Attribute);
        Assert.Equal(missingType ? GetProductTypeAttributeFailure.ProductTypeNotFound
            : GetProductTypeAttributeFailure.AttributeNotFound, result.Failure);
    }

    [Fact]
    public async Task RejectsInvalidInputBeforeRead()
    {
        var repository = new Repository();
        var useCase = new UseCase(repository);
        await Assert.ThrowsAsync<ArgumentNullException>(() => useCase.ExecuteAsync(null!, CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(default, AttributeDefinitionId.New()), CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => useCase.ExecuteAsync(new(ProductTypeId.New(), default), CancellationToken.None));
        Assert.Equal(0, repository.Reads);
    }

    [Fact]
    public async Task PreCancelledQueryDoesNotRead()
    {
        var repository = new Repository();
        using var source = new CancellationTokenSource();
        source.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => new UseCase(repository).ExecuteAsync(
            new(ProductTypeId.New(), AttributeDefinitionId.New()), source.Token));
        Assert.Equal(0, repository.Reads);
    }

    [Fact]
    public async Task RepositoryFailureIsNotReportedAsNotFound()
    {
        var expected = new InvalidOperationException("storage failure");
        var repository = new Repository { Error = expected };
        var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => new UseCase(repository).ExecuteAsync(
            new(ProductTypeId.New(), AttributeDefinitionId.New()), CancellationToken.None));
        Assert.Same(expected, actual);
    }

    [Fact]
    public void RejectsNullDependenciesAndSuccessfulPayload()
    {
        Assert.Throws<ArgumentNullException>(() => new UseCase(null!));
        Assert.Throws<ArgumentNullException>(() => GetProductTypeAttributeResult.Succeeded(null!));
    }

    private sealed class Repository : IProductTypeRepository
    {
        public ProductType? Type { get; set; } = ProductType.Create("Type");
        public Exception? Error { get; init; }
        public int Reads { get; private set; }
        public ProductTypeId RequestedId { get; private set; }
        public CancellationToken Token { get; private set; }

        public Task<ProductType?> GetByIdAsync(ProductTypeId id, CancellationToken cancellationToken)
        {
            Reads++;
            RequestedId = id;
            Token = cancellationToken;
            cancellationToken.ThrowIfCancellationRequested();
            return Error is null ? Task.FromResult(Type) : Task.FromException<ProductType?>(Error);
        }
    }
}
