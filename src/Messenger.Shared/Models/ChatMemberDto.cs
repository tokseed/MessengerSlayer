namespace Messenger.Shared.Models;

public sealed class ChatMemberDto
{
    public int ChatId { get; init; }
    public int UserId { get; init; }
    public DateTime ConnectedAt { get; init; }
}
