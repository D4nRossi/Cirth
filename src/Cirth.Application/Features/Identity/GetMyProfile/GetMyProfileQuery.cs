using Cirth.Application.Common.Ports;
using Cirth.Shared;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cirth.Application.Features.Identity.GetMyProfile;

public sealed record GetMyProfileQuery : IRequest<Result<MyProfileDto>>;

public sealed record MyProfileDto(
    string DisplayName,
    string Email,
    string Role,
    DateTimeOffset MemberSince,
    int DocumentCount,
    int CollectionCount,
    int TagCount,
    int ConversationCount,
    int TokensUsedToday,
    int DailyTokenLimit,
    int UploadsToday,
    int DailyUploadLimit,
    long StorageUsedBytes,
    long StorageLimitBytes);

internal sealed class GetMyProfileQueryHandler(
    ITenantProvider tenantProvider,
    IQueryDbContext db) : IRequestHandler<GetMyProfileQuery, Result<MyProfileDto>>
{
    public async Task<Result<MyProfileDto>> Handle(GetMyProfileQuery _, CancellationToken ct)
    {
        var userId = tenantProvider.CurrentUserId;

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
            return Result<MyProfileDto>.Failure(new("USER_NOT_FOUND", "Usuário não encontrado."));

        var quota = await db.UserQuotas.FirstOrDefaultAsync(q => q.Id == userId, ct);

        var docCount = await db.Documents.CountAsync(ct);
        var colCount = await db.Collections.CountAsync(ct);
        var tagCount = await db.Tags.CountAsync(ct);
        var convCount = await db.Conversations.CountAsync(ct);

        return Result<MyProfileDto>.Success(new(
            DisplayName: user.DisplayName,
            Email: user.Email,
            Role: user.Role.ToString(),
            MemberSince: user.CreatedAt,
            DocumentCount: docCount,
            CollectionCount: colCount,
            TagCount: tagCount,
            ConversationCount: convCount,
            TokensUsedToday: quota?.TokensUsedToday ?? 0,
            DailyTokenLimit: quota?.DailyTokenLimit ?? 100_000,
            UploadsToday: quota?.UploadsToday ?? 0,
            DailyUploadLimit: quota?.DailyUploadLimit ?? 50,
            StorageUsedBytes: quota?.StorageUsedBytes ?? 0,
            StorageLimitBytes: quota?.StorageLimitBytes ?? 5L * 1024 * 1024 * 1024));
    }
}
