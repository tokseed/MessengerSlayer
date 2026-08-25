namespace Messenger.Shared.Models;

public sealed class ChatDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string ChatType { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
