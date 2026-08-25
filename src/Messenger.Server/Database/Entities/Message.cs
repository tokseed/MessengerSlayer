namespace Messenger.Server.Database.Entities;

public sealed class Message
{
    public int Id { get; set; }
    public int ChatId { get; set; }
    public int SenderId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsEdited { get; set; }
    public int? ReplyToMessageId { get; set; }
    public DateTime SentAt { get; set; }
    public string Status { get; set; } = "sent_except";

    public Chat Chat { get; set; } = null!;
    public User Sender { get; set; } = null!;
}
