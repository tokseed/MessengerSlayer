using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Database.Entities;
using Messenger.Shared.Models;

namespace Messenger.Server.Services;

public sealed class ChatService
{
    private readonly MessengerDbContext _db;

    public ChatService(
        MessengerDbContext db)
    {
        _db =
            db ??
            throw new ArgumentNullException(
                nameof(db));
    }

    public async Task<ChatResult> CreateChatAsync(
        string title,
        string chatType,
        List<int> participantIds,
        CancellationToken cancellationToken = default)
    {
        Chat chat =
            new()
            {
                Title =
                    title,
                ChatType =
                    chatType,
                CreatedAt =
                    DateTime.UtcNow
            };

        _db.Chats.Add(
            chat);

        await _db.SaveChangesAsync(
            cancellationToken);

        foreach (int userId
                 in participantIds.Distinct())
        {
            _db.ChatMembers.Add(
                new ChatMember
                {
                    ChatId =
                        chat.Id,
                    UserId =
                        userId,
                    ConnectedAt =
                        DateTime.UtcNow
                });
        }

        await _db.SaveChangesAsync(
            cancellationToken);

        return new ChatResult
        {
            IsSuccess =
                true,
            ChatId =
                chat.Id
        };
    }

    public async Task<List<ChatDto>> GetChatsAsync(
        int userId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ChatMembers
            .Where(
                membership =>
                    membership.UserId ==
                    userId)
            .Select(
                membership =>
                    new ChatDto
                    {
                        Id =
                            membership.Chat.Id,
                        Title =
                            membership.Chat.Title,
                        ChatType =
                            membership.Chat.ChatType,
                        CreatedAt =
                            membership.Chat.CreatedAt,
                        AvatarUrl =
                            membership.Chat.ChatType ==
                            "direct"
                                ? membership.Chat.ChatMembers
                                    .Where(
                                        other =>
                                            other.UserId !=
                                            userId)
                                    .Select(
                                        other =>
                                            other.User.AvatarUrl)
                                    .FirstOrDefault()
                                : null
                    })
            .ToListAsync(
                cancellationToken);
    }

    public async Task<List<int>> GetChatMemberIdsAsync(
        int chatId,
        CancellationToken cancellationToken = default)
    {
        return await _db.ChatMembers
            .Where(
                membership =>
                    membership.ChatId ==
                    chatId)
            .Select(
                membership =>
                    membership.UserId)
            .ToListAsync(
                cancellationToken);
    }
}

public sealed class ChatResult
{
    public bool IsSuccess { get; init; }

    public int ChatId { get; init; }

    public string ErrorMessage { get; init; } =
        string.Empty;
}
