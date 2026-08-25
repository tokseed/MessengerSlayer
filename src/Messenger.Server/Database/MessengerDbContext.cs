using Microsoft.EntityFrameworkCore;
using Messenger.Server.Database.Entities;

namespace Messenger.Server.Database;

public sealed class MessengerDbContext : DbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Chat> Chats => Set<Chat>();
    public DbSet<ChatMember> ChatMembers => Set<ChatMember>();
    public DbSet<Message> Messages => Set<Message>();

    public MessengerDbContext(DbContextOptions<MessengerDbContext> options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Username).HasColumnName("username").HasMaxLength(50);
            entity.Property(e => e.FirstName).HasColumnName("first_name").HasMaxLength(50);
            entity.Property(e => e.LastName).HasColumnName("last_name").HasMaxLength(50);
            entity.Property(e => e.Email).HasColumnName("email").HasMaxLength(50);
            entity.Property(e => e.PhoneNumber).HasColumnName("numberphone").HasMaxLength(30);
            entity.Property(e => e.PasswordHash).HasColumnName("password_hash");
            entity.Property(e => e.AvatarUrl).HasColumnName("avatar_url");
            entity.Property(e => e.CreatedAt).HasColumnName("crated_at");
            entity.Property(e => e.LastSeenAt).HasColumnName("last_seen_at");
        });

        modelBuilder.Entity<Chat>(entity =>
        {
            entity.ToTable("chats");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Title).HasColumnName("title").HasMaxLength(100);
            entity.Property(e => e.ChatType).HasColumnName("chat_type").HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasColumnName("created_at");
        });

        modelBuilder.Entity<ChatMember>(entity =>
        {
            entity.ToTable("chat_members");
            entity.HasKey(e => new { e.UserId, e.ChatId });
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.UserId).HasColumnName("user_id");
            entity.Property(e => e.ConnectedAt).HasColumnName("connetion_at");

            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatMembers)
                .HasForeignKey(e => e.UserId);

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.ChatMembers)
                .HasForeignKey(e => e.ChatId);
        });

        modelBuilder.Entity<Message>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ChatId).HasColumnName("chat_id");
            entity.Property(e => e.SenderId).HasColumnName("sender_id");
            entity.Property(e => e.Content).HasColumnName("content");
            entity.Property(e => e.IsEdited).HasColumnName("edited");
            entity.Property(e => e.ReplyToMessageId).HasColumnName("reply_to_message_id");
            entity.Property(e => e.SentAt).HasColumnName("send_at");
            entity.Property(e => e.Status).HasColumnName("status").HasMaxLength(20);

            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatId);

            entity.HasOne(e => e.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(e => e.SenderId);
        });
    }
}
