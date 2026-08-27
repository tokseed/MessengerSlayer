using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Mesenger_server;

[Table("users")]
public class UserEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("first_name")]
    public string FirstName { get; set; } = string.Empty;

    [Column("last_name")]
    public string LastName { get; set; } = string.Empty;

    [Column("email")]
    public string? Email { get; set; }

    [Column("numberphone")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Column("password_hash")]
    public string PasswordHash { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Column("crated_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    [Column("last_seen_at")]
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}

[Table("chats")]
public class ChatEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("title")]
    public string Title { get; set; } = string.Empty;

    [Column("chat_type")]
    public string ChatType { get; set; } = "direct";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

[Table("messages")]
public class MessageEntity
{
    [Key]
    [Column("id")]
    public long Id { get; set; }

    [Column("chat_id")]
    public long ChatId { get; set; }

    [Column("sender_id")]
    public long SenderId { get; set; }

    [Column("content")]
    public string Content { get; set; } = string.Empty;

    [Column("status")]
    public string Status { get; set; } = "sent_except";

    [Column("edited")]
    public bool? Edited { get; set; }

    [Column("reply_to_message_id")]
    public long? ReplyToMessageId { get; set; }

    [Column("send_at")]
    public DateTime SendAt { get; set; } = DateTime.Now;
}

public class MessengerContext : DbContext
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ChatEntity> Chats => Set<ChatEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        // Подключаемся прямо к БД MessangerTop
        string connectionString = "Server=localhost,1433;" +
                                 "Database=MessangerTop;" +
                                 "User Id=sa;" +
                                 "Password=<YourStrongPassword123>;" +
                                 "Encrypt=False;" +
                                 "TrustServerCertificate=True;";

        optionsBuilder.UseSqlServer(connectionString);
    }
}