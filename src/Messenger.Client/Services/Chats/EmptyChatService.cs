using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Messenger.Client.UIModels;
namespace Messenger.Client.Services.Chats;
public sealed class EmptyChatService : IChatService
{
    public Task<IReadOnlyList<ChatListItem>> GetChatsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<ChatListItem> result = Array.Empty<ChatListItem>();
        return Task.FromResult(result);
    }
    public Task<IReadOnlyList<MessageItem>> GetMessagesAsync(long chatId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        IReadOnlyList<MessageItem> result = Array.Empty<MessageItem>();
        return Task.FromResult(result);
    }
    public Task<bool> CreateChatAsync(string title, IReadOnlyList<string> memberUsernames, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(false);
    }
    public Task SendMessageAsync(long chatId, string messageText, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
