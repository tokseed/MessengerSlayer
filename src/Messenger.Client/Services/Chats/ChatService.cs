using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Network;
using Messenger.Client.UIModels;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services.Chats;

public sealed class ChatService :
    IChatService
{
    private readonly IMessengerConnection _connection;
    private readonly ClientSessionState _session;

    public ChatService(
        IMessengerConnection connection,
        ClientSessionState session)
    {
        _connection =
            connection ??
            throw new ArgumentNullException(
                nameof(connection));

        _session =
            session ??
            throw new ArgumentNullException(
                nameof(session));

        _connection.PacketReceived +=
            OnPacketReceived;
    }

    public event EventHandler<MessageItem>?
        MessageReceived;

    public async Task<IReadOnlyList<ChatListItem>> GetChatsAsync(
        CancellationToken cancellationToken)
    {
        int userId =
            GetCurrentUserId();

        ChatListResponsePacket response =
            await _connection.SendRequestAsync<ChatListRequestPacket, ChatListResponsePacket>(
                new ChatListRequestPacket
                {
                    UserId =
                        userId
                },
                cancellationToken);

        return response.Chats
            .Select(
                chat =>
                    new ChatListItem
                    {
                        ChatId =
                            chat.Id,
                        CreatedAtUtc =
                            EnsureUtc(
                                chat.CreatedAt),
                        Title =
                            string.IsNullOrWhiteSpace(chat.Title)
                                ? $"Чат {chat.Id}"
                                : chat.Title,
                        Initials =
                            GetInitials(
                                chat.Title),
                        AvatarUrl =
                            chat.AvatarUrl,
                        LastActivityUtc =
                            EnsureUtc(
                                chat.CreatedAt),
                        IsOnline =
                            false
                    })
            .ToArray();
    }

    public async Task<IReadOnlyList<MessageItem>> GetMessagesAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        MessageHistoryResponsePacket response =
            await _connection.SendRequestAsync<MessageHistoryRequestPacket, MessageHistoryResponsePacket>(
                new MessageHistoryRequestPacket
                {
                    ChatId =
                        checked((int)chatId),
                    Limit =
                        200
                },
                cancellationToken);

        return response.Messages
            .OrderBy(
                message =>
                    message.SentAt)
            .Select(
                ToMessageItem)
            .ToArray();
    }

    public async Task<MessageItem?> GetLastMessageAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        MessageHistoryResponsePacket response =
            await _connection.SendRequestAsync<MessageHistoryRequestPacket, MessageHistoryResponsePacket>(
                new MessageHistoryRequestPacket
                {
                    ChatId =
                        checked((int)chatId),
                    Limit =
                        1
                },
                cancellationToken);

        MessageDto? message =
            response.Messages
                .OrderByDescending(
                    item =>
                        item.SentAt)
                .FirstOrDefault();

        return message == null
            ? null
            : ToMessageItem(
                message);
    }

    public async Task<bool> CreateChatAsync(
        string title,
        IReadOnlyList<string> memberUsernames,
        CancellationToken cancellationToken)
    {
        if (memberUsernames.Count == 0)
        {
            return false;
        }

        UserListResponsePacket usersResponse =
            await _connection.SendRequestAsync<UserListRequestPacket, UserListResponsePacket>(
                new UserListRequestPacket(),
                cancellationToken);

        List<UserDto> selectedUsers =
            new();

        foreach (string rawUsername in memberUsernames)
        {
            string username =
                rawUsername.Trim();

            UserDto? user =
                usersResponse.Users.FirstOrDefault(
                    item =>
                        string.Equals(
                            item.Username,
                            username,
                            StringComparison.OrdinalIgnoreCase));

            if (user == null ||
                user.Id == GetCurrentUserId())
            {
                return false;
            }

            if (selectedUsers.All(
                    item =>
                        item.Id != user.Id))
            {
                selectedUsers.Add(
                    user);
            }
        }

        if (selectedUsers.Count == 0)
        {
            return false;
        }

        string resolvedTitle =
            ResolveChatTitle(
                title,
                selectedUsers);

        ChatCreateResponsePacket response =
            await _connection.SendRequestAsync<ChatCreatePacket, ChatCreateResponsePacket>(
                new ChatCreatePacket
                {
                    Title =
                        resolvedTitle,
                    ChatType =
                        selectedUsers.Count == 1
                            ? "direct"
                            : "group",
                    ParticipantIds =
                        selectedUsers
                            .Select(
                                user =>
                                    user.Id)
                            .ToList()
                },
                cancellationToken);

        return response.Success;
    }

    public async Task SendMessageAsync(
        long chatId,
        string messageText,
        CancellationToken cancellationToken)
    {
        string content =
            messageText.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            return;
        }

        MessageAckPacket response =
            await _connection.SendRequestAsync<MessagePacket, MessageAckPacket>(
                new MessagePacket
                {
                    ChatId =
                        checked((int)chatId),
                    Content =
                        content
                },
                cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(
                response.Error);
        }

        MessageReceived?.Invoke(
            this,
            CreateLocalMessage(
                response.MessageId,
                chatId,
                content));
    }

    public async Task SendFileAsync(
        long chatId,
        string fileName,
        byte[] data,
        CancellationToken cancellationToken)
    {
        string envelope =
            AttachmentEnvelopeCodec.Encode(
                fileName,
                data);

        MessageAckPacket response =
            await _connection.SendRequestAsync<MessagePacket, MessageAckPacket>(
                new MessagePacket
                {
                    ChatId =
                        checked((int)chatId),
                    Content =
                        envelope
                },
                cancellationToken);

        if (!response.Success)
        {
            throw new InvalidOperationException(
                response.Error);
        }

        MessageReceived?.Invoke(
            this,
            CreateLocalMessage(
                response.MessageId,
                chatId,
                envelope));
    }

    private void OnPacketReceived(
        object? sender,
        Packet packet)
    {
        if (packet is not MessagePacket message)
        {
            return;
        }

        MessageReceived?.Invoke(
            this,
            CreateIncomingMessage(
                message));
    }

    private MessageItem ToMessageItem(
        MessageDto message)
    {
        return CreateMessageItem(
            message.Id,
            message.ChatId,
            message.SenderId,
            message.Content,
            EnsureUtc(
                message.SentAt),
            FormatStatus(
                message.Status));
    }

    private MessageItem CreateIncomingMessage(
        MessagePacket message)
    {
        return CreateMessageItem(
            messageId: 0,
            chatId: message.ChatId,
            senderId: message.SenderId,
            content: message.Content,
            sentAtUtc: DateTime.UtcNow,
            statusText: string.Empty);
    }

    private MessageItem CreateLocalMessage(
        int messageId,
        long chatId,
        string content)
    {
        return CreateMessageItem(
            messageId,
            chatId,
            GetCurrentUserId(),
            content,
            DateTime.UtcNow,
            "отправлено");
    }

    private MessageItem CreateMessageItem(
        long messageId,
        long chatId,
        int senderId,
        string content,
        DateTime sentAtUtc,
        string statusText)
    {
        bool isFile =
            AttachmentEnvelopeCodec.TryDecode(
                content,
                out AttachmentEnvelope? attachment,
                out byte[] fileData);

        return new MessageItem
        {
            MessageId =
                messageId,
            ChatId =
                chatId,
            SenderId =
                senderId,
            Text =
                isFile
                    ? string.Empty
                    : content,
            SentAtUtc =
                sentAtUtc,
            IsOwn =
                _session.UserId ==
                senderId,
            StatusText =
                statusText,
            IsFile =
                isFile,
            FileName =
                attachment?.FileName ??
                string.Empty,
            FileSizeText =
                isFile
                    ? FormatFileSize(
                        fileData.LongLength)
                    : string.Empty,
            FileData =
                fileData
        };
    }

    private int GetCurrentUserId()
    {
        return _session.UserId
            ?? throw new InvalidOperationException(
                "The user is not authenticated.");
    }

    private string ResolveChatTitle(
        string title,
        IReadOnlyList<UserDto> users)
    {
        string trimmed =
            title.Trim();

        if (!string.IsNullOrWhiteSpace(trimmed))
        {
            return trimmed;
        }

        IEnumerable<string> participantNames =
            users
                .Take(3)
                .Select(
                    user =>
                    {
                        string fullName =
                            $"{user.FirstName} {user.LastName}".Trim();

                        return string.IsNullOrWhiteSpace(fullName)
                            ? user.Username
                            : fullName;
                    });

        // The team server stores one shared title for a direct chat.
        // Include both sides so the invited user does not see only their
        // own name when the creator leaves the title empty.
        return string.Join(
            ", ",
            new[] { _session.Username }
                .Concat(participantNames));
    }

    private static string GetInitials(
        string title)
    {
        string[] parts =
            title.Split(
                ' ',
                StringSplitOptions.RemoveEmptyEntries |
                StringSplitOptions.TrimEntries);

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        if (parts.Length == 1)
        {
            return parts[0][..1]
                .ToUpperInvariant();
        }

        return string.Concat(
            parts[0][..1],
            parts[1][..1])
            .ToUpperInvariant();
    }

    private static string FormatFileSize(
        long bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} Б";
        }

        if (bytes < 1024 * 1024)
        {
            return $"{bytes / 1024d:0.#} КБ";
        }

        return $"{bytes / 1024d / 1024d:0.##} МБ";
    }

    private static string FormatStatus(
        string status)
    {
        return status switch
        {
            "sent_except" =>
                "отправлено",
            "delivered" =>
                "доставлено",
            "is_read" =>
                "прочитано",
            _ =>
                status
        };
    }

    private static DateTime EnsureUtc(
        DateTime value)
    {
        return value.Kind switch
        {
            DateTimeKind.Utc =>
                value,
            DateTimeKind.Local =>
                value.ToUniversalTime(),
            _ =>
                DateTime.SpecifyKind(
                    value,
                    DateTimeKind.Utc)
        };
    }
}
