using Cirth.Application.Common.Ports;
using Cirth.Application.Features.Documents.UploadDocument;
using Cirth.Domain.Documents;
using Cirth.Shared;
using FluentAssertions;
using NSubstitute;

namespace Cirth.Application.Tests.Documents;

public sealed class UploadDocumentCommandTests
{
    private readonly ITenantProvider _tenantProvider = Substitute.For<ITenantProvider>();
    private readonly IObjectStorage _objectStorage = Substitute.For<IObjectStorage>();
    private readonly IJobQueue _jobQueue = Substitute.For<IJobQueue>();
    private readonly IDocumentRepository _documentRepository = Substitute.For<IDocumentRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    public UploadDocumentCommandTests()
    {
        _tenantProvider.CurrentTenantId.Returns(new TenantId(Guid.NewGuid()));
        _tenantProvider.CurrentUserId.Returns(new UserId(Guid.NewGuid()));
        _objectStorage.PutAsync(default!, default!, default!, default!, default)
            .ReturnsForAnyArgs("stored-key");
    }

    [Fact]
    public async Task Handle_ValidPdfUpload_ShouldCreateDocumentAndEnqueueJob()
    {
        var handler = new UploadDocumentCommandHandler(
            _tenantProvider, _objectStorage, _jobQueue, _documentRepository, _uow);

        var content = new MemoryStream([0x25, 0x50, 0x44, 0x46]); // %PDF header
        var cmd = new UploadDocumentCommand(
            "Test PDF", "test.pdf", content, "application/pdf", 1024, null, false, null);

        var result = await handler.Handle(cmd, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.DocumentId.Should().NotBe(Guid.Empty);

        await _documentRepository.Received(1).AddAsync(
            Arg.Is<Document>(d => d.Title == "Test PDF" && d.SourceType == DocumentSourceType.Pdf),
            Arg.Any<CancellationToken>());

        await _jobQueue.Received(1).EnqueueAsync(
            "ProcessDocument", Arg.Any<object>(), Arg.Any<CancellationToken>());

        await _uow.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FileTooLarge_ShouldReturnValidationError()
    {
        var validator = new UploadDocumentCommandValidator();
        var cmd = new UploadDocumentCommand(
            "Big file", "big.pdf", Stream.Null, "application/pdf",
            51L * 1024 * 1024, null, false, null);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("50 MB"));
    }

    [Fact]
    public async Task Handle_InvalidMimeType_ShouldReturnValidationError()
    {
        var validator = new UploadDocumentCommandValidator();
        var cmd = new UploadDocumentCommand(
            "Video", "video.mp4", Stream.Null, "video/mp4", 1024, null, false, null);

        var result = await validator.ValidateAsync(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("Unsupported"));
    }
}
