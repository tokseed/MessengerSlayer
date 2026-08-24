using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Messenger.Client.UIModels;
namespace Messenger.Client.Services.Chats;
public interface IChatService
{
    Task<IReadOnlyList<ChatListItem>> GetChatsAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<MessageItem>> GetMessagesAsync(long chatId, CancellationToken cancellationToken);
    Task<bool> CreateChatAsync(string title, IReadOnlyList<string> memberUsernames, CancellationToken cancellationToken);
    Task SendMessageAsync(long chatId, string messageText, CancellationToken cancellationToken);
}
