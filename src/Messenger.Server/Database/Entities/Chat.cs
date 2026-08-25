namespace Messenger.Server.Database.Entities;

public sealed class Chat
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ChatType { get; set; } = "direct";
    public DateTime CreatedAt { get; set; }

    public List<ChatMember> ChatMembers { get; set; } = [];
    public List<Message> Messages { get; set; } = [];
}
