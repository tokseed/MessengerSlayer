using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database;
using Messenger.Server.Database.Entities;
using Messenger.Shared.Models;

namespace Messenger.Server.Services;

public sealed class MessageService
{
    private readonly MessengerDbContext _db;

    public MessageService(MessengerDbContext db)
    {
        _db = db;
    }

    public async Task<MessageResult> SendMessageAsync(
        int senderId, int chatId, string content, int? replyToMessageId,
        CancellationToken cancellationToken = default)
    {
        var isMember = await _db.ChatMembers
            .AnyAsync(cm => cm.ChatId == chatId && cm.UserId == senderId, cancellationToken);

        if (!isMember)
            return new MessageResult { IsSuccess = false, ErrorMessage = "Not a member of this chat" };

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            ReplyToMessageId = replyToMessageId,
            SentAt = DateTime.UtcNow,
            Status = "sent_except"
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return new MessageResult { IsSuccess = true, MessageId = message.Id };
    }

    public async Task<List<MessageDto>> GetMessagesAsync(int chatId, int limit = 50, CancellationToken cancellationToken = default)
    {
        return await _db.Messages
            .Where(m => m.ChatId == chatId)
            .OrderByDescending(m => m.SentAt)
            .Take(limit)
            .Select(m => new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                Content = m.Content,
                IsEdited = m.IsEdited,
                ReplyToMessageId = m.ReplyToMessageId,
                SentAt = m.SentAt,
                Status = m.Status
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class MessageResult
{
    public bool IsSuccess { get; init; }
    public int MessageId { get; init; }
    public string ErrorMessage { get; init; } = string.Empty;
}
