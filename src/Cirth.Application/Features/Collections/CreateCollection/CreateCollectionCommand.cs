using Cirth.Application.Common.Ports;
using Cirth.Domain.Collections;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.Collections.CreateCollection;

public sealed record CreateCollectionCommand(string Name, string? Description = null) : IRequest<Result<CollectionDto>>;

public sealed record CollectionDto(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt);

public sealed class CreateCollectionCommandValidator : AbstractValidator<CreateCollectionCommand>
{
    public CreateCollectionCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
    }
}

internal sealed class CreateCollectionCommandHandler(
    ITenantProvider tenantProvider,
    ICollectionRepository collectionRepo,
    IUnitOfWork uow) : IRequestHandler<CreateCollectionCommand, Result<CollectionDto>>
{
    public async Task<Result<CollectionDto>> Handle(CreateCollectionCommand cmd, CancellationToken ct)
    {
        var col = Collection.Create(tenantProvider.CurrentTenantId, cmd.Name, cmd.Description);
        await collectionRepo.AddAsync(col, ct);
        await uow.CommitAsync(ct);
        return Result<CollectionDto>.Success(new(col.Id.Value, col.Name, col.Description, col.CreatedAt));
    }
}
