using Cirth.Application.Common.Ports;
using Cirth.Domain.Tags;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.Tags.CreateTag;

public sealed record CreateTagCommand(string Name, string? Color = null) : IRequest<Result<TagDto>>;

public sealed record TagDto(Guid Id, string Name, string? Color);

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Color).Matches("^#[0-9A-Fa-f]{6}$").When(x => x.Color is not null)
            .WithMessage("Color must be a valid hex color (#RRGGBB).");
    }
}

internal sealed class CreateTagCommandHandler(
    ITenantProvider tenantProvider,
    ITagRepository tagRepo,
    IUnitOfWork uow) : IRequestHandler<CreateTagCommand, Result<TagDto>>
{
    public async Task<Result<TagDto>> Handle(CreateTagCommand cmd, CancellationToken ct)
    {
        var tenantId = tenantProvider.CurrentTenantId;
        var existing = await tagRepo.GetByNameAsync(tenantId, cmd.Name, ct);
        if (existing is not null)
            return Error.Conflict("tag.already_exists", $"Tag '{cmd.Name}' already exists.");

        var tag = Tag.Create(tenantId, cmd.Name, cmd.Color);
        await tagRepo.AddAsync(tag, ct);
        await uow.CommitAsync(ct);
        return Result<TagDto>.Success(new(tag.Id.Value, tag.Name, tag.Color));
    }
}
