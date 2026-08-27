using Messenger.Client.UIModels;

namespace Messenger.Client.Services.Chats;

public interface IChatService
{
    event EventHandler<MessageItem>?
        MessageReceived;

    Task<IReadOnlyList<ChatListItem>> GetChatsAsync(
        CancellationToken cancellationToken);

    Task<IReadOnlyList<MessageItem>> GetMessagesAsync(
        long chatId,
        CancellationToken cancellationToken);

    Task<MessageItem?> GetLastMessageAsync(
        long chatId,
        CancellationToken cancellationToken);

    Task<bool> CreateChatAsync(
        string title,
        IReadOnlyList<string> memberUsernames,
        CancellationToken cancellationToken);

    Task SendMessageAsync(
        long chatId,
        string messageText,
        CancellationToken cancellationToken);

    Task SendFileAsync(
        long chatId,
        string fileName,
        byte[] data,
        CancellationToken cancellationToken);
}
