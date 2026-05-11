using Cirth.Application.Common.Ports;
using Cirth.Domain.Auth;
using Cirth.Shared;
using FluentValidation;
using MediatR;

namespace Cirth.Application.Features.ApiKeys.CreateApiKey;

public sealed record CreateApiKeyCommand(string Name, DateTimeOffset? ExpiresAt = null) : IRequest<Result<ApiKeyCreatedDto>>;

public sealed record ApiKeyCreatedDto(Guid Id, string Name, string PlainKey, DateTimeOffset? ExpiresAt, DateTimeOffset CreatedAt);

public sealed class CreateApiKeyCommandValidator : AbstractValidator<CreateApiKeyCommand>
{
    public CreateApiKeyCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
    }
}

internal sealed class CreateApiKeyCommandHandler(
    ITenantProvider tenantProvider,
    IApiKeyRepository apiKeyRepo,
    IApiKeyHasher hasher,
    IUnitOfWork uow) : IRequestHandler<CreateApiKeyCommand, Result<ApiKeyCreatedDto>>
{
    public async Task<Result<ApiKeyCreatedDto>> Handle(CreateApiKeyCommand cmd, CancellationToken ct)
    {
        var plainKey = hasher.Generate();
        var hash = hasher.Hash(plainKey);

        var apiKey = ApiKey.Create(
            tenantProvider.CurrentTenantId,
            tenantProvider.CurrentUserId,
            cmd.Name, hash, cmd.ExpiresAt);

        await apiKeyRepo.AddAsync(apiKey, ct);
        await uow.CommitAsync(ct);

        return Result<ApiKeyCreatedDto>.Success(new(
            apiKey.Id.Value, apiKey.Name, plainKey, apiKey.ExpiresAt, apiKey.CreatedAt));
    }
}
