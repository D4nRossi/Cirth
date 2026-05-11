using Cirth.Application.Common.Ports;
using Cirth.Application.Features.Tags.CreateTag;
using Cirth.Domain.Tags;
using Cirth.Shared;
using FluentAssertions;
using NSubstitute;

namespace Cirth.Application.Tests.Tags;

public sealed class CreateTagCommandTests
{
    private readonly ITenantProvider _tenantProvider = Substitute.For<ITenantProvider>();
    private readonly ITagRepository _tagRepo = Substitute.For<ITagRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public CreateTagCommandTests()
    {
        _tenantProvider.CurrentTenantId.Returns(new TenantId(Guid.NewGuid()));
        _tagRepo.GetByNameAsync(Arg.Any<TenantId>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((Tag?)null);
    }

    [Fact]
    public async Task Handle_NewTag_ShouldCreateAndReturnTag()
    {
        var handler = new CreateTagCommandHandler(_tenantProvider, _tagRepo, _uow);
        var result = await handler.Handle(new CreateTagCommand("machine-learning", "#C9A961"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("machine-learning");
        await _tagRepo.Received(1).AddAsync(Arg.Any<Tag>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicateTag_ShouldReturnConflictError()
    {
        _tagRepo.GetByNameAsync(Arg.Any<TenantId>(), "existing", Arg.Any<CancellationToken>())
            .Returns(Tag.Create(new TenantId(Guid.NewGuid()), "existing"));

        var handler = new CreateTagCommandHandler(_tenantProvider, _tagRepo, _uow);
        var result = await handler.Handle(new CreateTagCommand("existing"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error!.Type.Should().Be(ErrorType.Conflict);
    }

    [Fact]
    public async Task Validator_InvalidColor_ShouldFail()
    {
        var validator = new CreateTagCommandValidator();
        var result = await validator.ValidateAsync(new CreateTagCommand("tag", "red"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("hex"));
    }
}
