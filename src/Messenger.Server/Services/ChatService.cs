using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Database.Entities;
using Messenger.Shared.Models;

namespace Messenger.Server.Services;

public sealed class ChatService
{
    private readonly MessengerDbContext _db;

    public ChatService(MessengerDbContext db)
    {
        _db = db;
    }

    public async Task<ChatResult> CreateChatAsync(
        string title, string chatType, List<int> participantIds,
        CancellationToken cancellationToken = default)
    {
        var chat = new Chat
        {
            Title = title,
            ChatType = chatType,
            CreatedAt = DateTime.UtcNow
        };

        _db.Chats.Add(chat);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var userId in participantIds)
        {
            _db.ChatMembers.Add(new ChatMember
            {
                ChatId = chat.Id,
                UserId = userId,
                ConnectedAt = DateTime.UtcNow
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new ChatResult { IsSuccess = true, ChatId = chat.Id };
    }

    public async Task<List<ChatDto>> GetChatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _db.ChatMembers
            .Where(cm => cm.UserId == userId)
            .Select(cm => new ChatDto
            {
                Id = cm.Chat.Id,
                Title = cm.Chat.Title,
                ChatType = cm.Chat.ChatType,
                CreatedAt = cm.Chat.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<int>> GetChatMemberIdsAsync(int chatId, CancellationToken cancellationToken = default)
    {
        return await _db.ChatMembers
            .Where(cm => cm.ChatId == chatId)
            .Select(cm => cm.UserId)
            .ToListAsync(cancellationToken);
    }
}

public sealed class ChatResult
{
    public bool IsSuccess { get; init; }
    public int ChatId { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
