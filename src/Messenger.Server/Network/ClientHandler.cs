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
    private int? _currentUserId;

    public bool IsConnected => _client.Connected;
    public int? CurrentUserId => _currentUserId;

    public Func<int, Packet, int?, CancellationToken, Task>? OnBroadcast { get; set; }

    public ClientHandler(
        TcpClient client,
        Stream stream,
        AuthService authService,
        MessageService messageService,
        ChatService chatService)
    {
        _client = client;
        _stream = stream;
        _authService = authService;
        _messageService = messageService;
        _chatService = chatService;
    }

    public async Task HandleAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (_client.Connected && !cancellationToken.IsCancellationRequested)
            {
                var packet = await PacketSerializer.ReceiveAsync(_stream, cancellationToken);
                if (packet == null)
                    break;

                var response = await ProcessPacketAsync(packet, cancellationToken);
                if (response != null)
                    await PacketSerializer.SendAsync(_stream, response, cancellationToken);
            }
        }
        catch (Exception)
        {
            // Client disconnected
        }
        finally
        {
            Disconnect();
        }
    }

    private async Task<Packet?> ProcessPacketAsync(Packet packet, CancellationToken cancellationToken)
    {
        return packet.Type switch
        {
            PacketType.Auth => await HandleAuthAsync((AuthPacket)packet, cancellationToken),
            PacketType.Register => await HandleRegisterAsync((RegisterPacket)packet, cancellationToken),
            PacketType.Message => await HandleMessageAsync((MessagePacket)packet, cancellationToken),
            PacketType.ChatCreate => await HandleChatCreateAsync((ChatCreatePacket)packet, cancellationToken),
            PacketType.ChatListRequest => await HandleChatListAsync((ChatListRequestPacket)packet, cancellationToken),
            PacketType.UserListRequest => await HandleUserListAsync(cancellationToken),
            PacketType.MessageHistoryRequest => await HandleMessageHistoryAsync((MessageHistoryRequestPacket)packet, cancellationToken),
            PacketType.Disconnect => HandleDisconnect(),
            _ => null
        };
    }

    private async Task<AuthResponsePacket> HandleAuthAsync(AuthPacket packet, CancellationToken cancellationToken)
    {
        var result = await _authService.AuthenticateAsync(packet.Username, packet.PasswordHash, cancellationToken);
        if (result.IsSuccess)
            _currentUserId = result.UserId;

        return new AuthResponsePacket
        {
            Success = result.IsSuccess,
            UserId = result.UserId,
            Error = result.ErrorMessage
        };
    }

    private async Task<RegisterResponsePacket> HandleRegisterAsync(RegisterPacket packet, CancellationToken cancellationToken)
    {
        var result = await _authService.RegisterAsync(
            packet.Username, packet.PasswordHash,
            packet.FirstName, packet.LastName, packet.PhoneNumber,
            cancellationToken);

        return new RegisterResponsePacket
        {
            Success = result.IsSuccess,
            UserId = result.UserId,
            Error = result.ErrorMessage
        };
    }

    private async Task<MessageAckPacket> HandleMessageAsync(MessagePacket packet, CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
            return new MessageAckPacket { Success = false, Error = "Not authenticated" };

        var result = await _messageService.SendMessageAsync(
            _currentUserId.Value, packet.ChatId,
            packet.Content, packet.ReplyToMessageId,
            cancellationToken);

        if (result.IsSuccess && OnBroadcast != null)
        {
            var broadcastPacket = new MessagePacket
            {
                SenderId = _currentUserId.Value,
                ChatId = packet.ChatId,
                Content = packet.Content,
                ReplyToMessageId = packet.ReplyToMessageId
            };
            await OnBroadcast(packet.ChatId, broadcastPacket, _currentUserId, cancellationToken);
        }

        return new MessageAckPacket
        {
            Success = result.IsSuccess,
            MessageId = result.MessageId,
            Error = result.ErrorMessage
        };
    }

    private async Task<ChatCreateResponsePacket> HandleChatCreateAsync(ChatCreatePacket packet, CancellationToken cancellationToken)
    {
        if (_currentUserId == null)
            return new ChatCreateResponsePacket { Success = false, Error = "Not authenticated" };

        var participantIds = new List<int>(packet.ParticipantIds) { _currentUserId.Value };
        var result = await _chatService.CreateChatAsync(
            packet.Title, packet.ChatType, participantIds,
            cancellationToken);

        return new ChatCreateResponsePacket
        {
            Success = result.IsSuccess,
            ChatId = result.ChatId,
            Error = result.ErrorMessage
        };
    }

    private async Task<ChatListResponsePacket> HandleChatListAsync(ChatListRequestPacket packet, CancellationToken cancellationToken)
    {
        var chats = await _chatService.GetChatsAsync(packet.UserId, cancellationToken);
        return new ChatListResponsePacket { Chats = chats };
    }

    private async Task<UserListResponsePacket> HandleUserListAsync(CancellationToken cancellationToken)
    {
        var users = await _authService.GetUsersAsync(cancellationToken);
        return new UserListResponsePacket { Users = users };
    }

    private async Task<MessageHistoryResponsePacket> HandleMessageHistoryAsync(MessageHistoryRequestPacket packet, CancellationToken cancellationToken)
    {
        var messages = await _messageService.GetMessagesAsync(packet.ChatId, packet.Limit, cancellationToken);
        return new MessageHistoryResponsePacket { Messages = messages };
    }

    private DisconnectPacket HandleDisconnect()
    {
        Disconnect();
        return new DisconnectPacket();
    }

    public void Disconnect()
    {
        try { _stream.Close(); } catch { }
        try { _client.Close(); } catch { }
    }

    public async Task SendPacketAsync(Packet packet, CancellationToken cancellationToken = default)
    {
        await PacketSerializer.SendAsync(_stream, packet, cancellationToken);
    }
}
