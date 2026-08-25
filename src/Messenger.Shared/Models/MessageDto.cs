namespace Messenger.Shared.Models;

public sealed class MessageDto
{
    public int Id { get; init; }
    public int ChatId { get; init; }
    public int SenderId { get; init; }
    public string Content { get; init; } = string.Empty;
    public bool IsEdited { get; init; }
    public int? ReplyToMessageId { get; init; }
    public DateTime SentAt { get; init; }
    public string Status { get; init; } = string.Empty;
}
