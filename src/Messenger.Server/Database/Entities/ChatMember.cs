namespace Messenger.Server.Database.Entities;

public sealed class ChatMember
{
    public int ChatId { get; set; }
    public int UserId { get; set; }
    public DateTime ConnectedAt { get; set; }

    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
}
