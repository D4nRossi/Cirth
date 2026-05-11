using Cirth.Application.Common.Ports;
using Cirth.Domain.Auth;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.Identity.InviteUser;

public sealed record InviteUserCommand(string Email) : IRequest<Result<InviteDto>>;

public sealed record InviteDto(string Token, string InviteUrl, DateTimeOffset ExpiresAt);

public sealed class InviteUserCommandValidator : AbstractValidator<InviteUserCommand>
{
    public InviteUserCommandValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(320);
    }
}

internal sealed class InviteUserCommandHandler(
    ITenantProvider tenantProvider,
    IUserInviteRepository inviteRepo,
    IUnitOfWork uow) : IRequestHandler<InviteUserCommand, Result<InviteDto>>
{
    public async Task<Result<InviteDto>> Handle(InviteUserCommand cmd, CancellationToken ct)
    {
        var invite = UserInvite.Create(
            tenantProvider.CurrentTenantId,
            tenantProvider.CurrentUserId,
            cmd.Email);

        await inviteRepo.AddAsync(invite, ct);
        await uow.CommitAsync(ct);

        return Result<InviteDto>.Success(new(
            invite.Token,
            $"/invite/{invite.Token}",
            invite.ExpiresAt));
    }
}
