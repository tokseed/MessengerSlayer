using System.Net.Sockets;
using Messenger.Server.Services;
using Messenger.Shared.Network;
using Messenger.Shared.Packets;

namespace Messenger.Server.Network;

public sealed class ClientHandler
{
    private readonly TcpClient _client;
    private readonly Stream _stream;
    private readonly AuthService _authService;
    private readonly MessageService _messageService;
    private readonly ChatService _chatService;

    private readonly SemaphoreSlim _sendLock =
        new(1, 1);

    private int? _currentUserId;

    public bool IsConnected =>
        _client.Connected;

    public int? CurrentUserId =>
        _currentUserId;

    public Func<int, Packet, int?, CancellationToken, Task>?
        OnBroadcast
    {
        get;
        set;
    }

    public ClientHandler(
        TcpClient client,
        Stream stream,
        AuthService authService,
        MessageService messageService,
        ChatService chatService)
    {
        _client =
            client ??
            throw new ArgumentNullException(
                nameof(client));

        _stream =
            stream ??
            throw new ArgumentNullException(
                nameof(stream));

        _authService =
            authService ??
            throw new ArgumentNullException(
                nameof(authService));

        _messageService =
            messageService ??
            throw new ArgumentNullException(
                nameof(messageService));

        _chatService =
            chatService ??
            throw new ArgumentNullException(
                nameof(chatService));
    }

    public async Task HandleAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (_client.Connected &&
                   !cancellationToken.IsCancellationRequested)
            {
                Packet? packet =
                    await PacketSerializer.ReceiveAsync(
                        _stream,
                        cancellationToken);

                if (packet == null)
                {
                    break;
                }

                Packet? response =
                    await ProcessPacketAsync(
                        packet,
                        cancellationToken);

                if (response != null)
                {
                    await SendPacketAsync(
                        response,
                        cancellationToken);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception exception)
        {
            Console.WriteLine(
                $"Client session error " +
                $"(UserId={_currentUserId?.ToString() ?? "anonymous"}): " +
                exception);
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task<Packet?> ProcessPacketAsync(
        Packet packet,
        CancellationToken cancellationToken)
    {
        return packet.Type switch
        {
            PacketType.Auth =>
                await HandleAuthAsync(
                    (AuthPacket)packet,
                    cancellationToken),

            PacketType.Register =>
                await HandleRegisterAsync(
                    (RegisterPacket)packet,
                    cancellationToken),

            PacketType.ProfileRequest =>
                await HandleProfileRequestAsync(
                    cancellationToken),

            PacketType.ProfileUpdate =>
                await HandleProfileUpdateAsync(
                    (ProfileUpdatePacket)packet,
                    cancellationToken),

            PacketType.Message =>
                await HandleMessageAsync(
                    (MessagePacket)packet,
                    cancellationToken),

            PacketType.ChatCreate =>
                await HandleChatCreateAsync(
                    (ChatCreatePacket)packet,
                    cancellationToken),

            PacketType.ChatListRequest =>
                await HandleChatListAsync(
                    (ChatListRequestPacket)packet,
                    cancellationToken),

            PacketType.UserListRequest =>
                await HandleUserListAsync(
                    cancellationToken),

            PacketType.MessageHistoryRequest =>
                await HandleMessageHistoryAsync(
                    (MessageHistoryRequestPacket)packet,
                    cancellationToken),

            PacketType.Disconnect =>
                HandleDisconnect(),

            _ =>
                null
        };
    }

    private async Task<AuthResponsePacket> HandleAuthAsync(
        AuthPacket packet,
        CancellationToken cancellationToken)
    {
        AuthResult result =
            await _authService.AuthenticateAsync(
                packet.Username,
                packet.PasswordHash,
                cancellationToken);

        if (result.IsSuccess)
        {
            _currentUserId =
                result.UserId;

            Console.WriteLine(
                $"Client authenticated: UserId={result.UserId}");
        }

        return new AuthResponsePacket
        {
            Success =
                result.IsSuccess,
            UserId =
                result.UserId,
            Error =
                result.ErrorMessage
        };
    }

    private async Task<RegisterResponsePacket> HandleRegisterAsync(
        RegisterPacket packet,
        CancellationToken cancellationToken)
    {
        AuthResult result =
            await _authService.RegisterAsync(
                packet.Username,
                packet.PasswordHash,
                packet.FirstName,
                packet.LastName,
                packet.PhoneNumber,
                packet.Email,
                packet.AvatarUrl,
                cancellationToken);

        Console.WriteLine(
            result.IsSuccess
                ? $"User registered: UserId={result.UserId}, Username={packet.Username}"
                : $"User registration rejected: {result.ErrorMessage}");

        return new RegisterResponsePacket
        {
            Success =
                result.IsSuccess,
            UserId =
                result.UserId,
            Error =
                result.ErrorMessage
        };
    }


    private async Task<ProfileResponsePacket> HandleProfileRequestAsync(
        CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
        {
            return new ProfileResponsePacket
            {
                Success =
                    false,
                Error =
                    "Not authenticated"
            };
        }

        Messenger.Shared.Models.UserDto? user =
            await _authService.GetUserAsync(
                _currentUserId.Value,
                cancellationToken);

        return new ProfileResponsePacket
        {
            Success =
                user != null,
            User =
                user,
            Error =
                user == null
                    ? "User not found"
                    : string.Empty
        };
    }

    private async Task<ProfileUpdateResponsePacket> HandleProfileUpdateAsync(
        ProfileUpdatePacket packet,
        CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
        {
            return new ProfileUpdateResponsePacket
            {
                Success =
                    false,
                Error =
                    "Not authenticated"
            };
        }

        ProfileResult result =
            await _authService.UpdateProfileAsync(
                _currentUserId.Value,
                packet.Email,
                packet.AvatarUrl,
                cancellationToken);

        return new ProfileUpdateResponsePacket
        {
            Success =
                result.IsSuccess,
            User =
                result.User,
            Error =
                result.ErrorMessage
        };
    }

    private async Task<MessageAckPacket> HandleMessageAsync(
        MessagePacket packet,
        CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
        {
            return new MessageAckPacket
            {
                Success =
                    false,
                Error =
                    "Not authenticated"
            };
        }

        MessageResult result =
            await _messageService.SendMessageAsync(
                _currentUserId.Value,
                packet.ChatId,
                packet.Content,
                packet.ReplyToMessageId,
                cancellationToken);

        if (result.IsSuccess &&
            OnBroadcast != null)
        {
            MessagePacket broadcastPacket =
                new()
                {
                    SenderId =
                        _currentUserId.Value,
                    ChatId =
                        packet.ChatId,
                    Content =
                        packet.Content,
                    ReplyToMessageId =
                        packet.ReplyToMessageId
                };

            await OnBroadcast(
                packet.ChatId,
                broadcastPacket,
                _currentUserId,
                cancellationToken);
        }

        return new MessageAckPacket
        {
            Success =
                result.IsSuccess,
            MessageId =
                result.MessageId,
            Error =
                result.ErrorMessage
        };
    }

    private async Task<ChatCreateResponsePacket> HandleChatCreateAsync(
        ChatCreatePacket packet,
        CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
        {
            return new ChatCreateResponsePacket
            {
                Success =
                    false,
                Error =
                    "Not authenticated"
            };
        }

        List<int> participantIds =
            new(packet.ParticipantIds)
            {
                _currentUserId.Value
            };

        ChatResult result =
            await _chatService.CreateChatAsync(
                packet.Title,
                packet.ChatType,
                participantIds,
                cancellationToken);

        return new ChatCreateResponsePacket
        {
            Success =
                result.IsSuccess,
            ChatId =
                result.ChatId,
            Error =
                result.ErrorMessage
        };
    }

    private async Task<ChatListResponsePacket> HandleChatListAsync(
        ChatListRequestPacket packet,
        CancellationToken cancellationToken)
    {
        List<Messenger.Shared.Models.ChatDto> chats =
            await _chatService.GetChatsAsync(
                packet.UserId,
                cancellationToken);

        return new ChatListResponsePacket
        {
            Chats =
                chats
        };
    }

    private async Task<UserListResponsePacket> HandleUserListAsync(
        CancellationToken cancellationToken)
    {
        List<Messenger.Shared.Models.UserDto> users =
            await _authService.GetUsersAsync(
                cancellationToken);

        return new UserListResponsePacket
        {
            Users =
                users
        };
    }

    private async Task<MessageHistoryResponsePacket> HandleMessageHistoryAsync(
        MessageHistoryRequestPacket packet,
        CancellationToken cancellationToken)
    {
        List<Messenger.Shared.Models.MessageDto> messages =
            await _messageService.GetMessagesAsync(
                packet.ChatId,
                packet.Limit,
                cancellationToken);

        return new MessageHistoryResponsePacket
        {
            Messages =
                messages
        };
    }

    private DisconnectPacket HandleDisconnect()
    {
        Disconnect();

        return new DisconnectPacket();
    }

    public void Disconnect()
    {
        try
        {
            _stream.Close();
        }
        catch
        {
        }

        try
        {
            _client.Close();
        }
        catch
        {
        }
    }

    public async Task SendPacketAsync(
        Packet packet,
        CancellationToken cancellationToken = default)
    {
        await _sendLock.WaitAsync(
            cancellationToken);

        try
        {
            await PacketSerializer.SendAsync(
                _stream,
                packet,
                cancellationToken);
        }
        finally
        {
            _sendLock.Release();
        }
    }
}
