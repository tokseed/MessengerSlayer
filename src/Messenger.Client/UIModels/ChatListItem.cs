namespace Messenger.Client.UIModels;
public sealed class ChatListItem
{
    public long ChatId { get; init; }
    public string Title { get; init; } = string.Empty;
    public string LastMessage { get; init; } = string.Empty;
    public string TimeText { get; init; } = string.Empty;
    public string Initials { get; init; } = string.Empty;
    public int UnreadCount { get; init; }
    public bool HasUnreadMessages => UnreadCount > 0;
    public bool IsOnline { get; init; }
}
