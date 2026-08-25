using Messenger.Client.Network;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services;

public sealed class ChatService
{
    private readonly TcpClientService _client;

    public ChatService(TcpClientService client)
    {
        _client = client;
    }

    public async Task<ChatCreateResponsePacket> CreateAsync(string title, string chatType, List<int> participantIds, CancellationToken cancellationToken = default)
    {
        var packet = new ChatCreatePacket
        {
            Title = title,
            ChatType = chatType,
            ParticipantIds = participantIds
        };

        return (ChatCreateResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
    }

    public async Task<List<ChatDto>> GetChatsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var packet = new ChatListRequestPacket { UserId = userId };
        var response = (ChatListResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
        return response.Chats;
    }

    public async Task<List<MessageDto>> GetHistoryAsync(int chatId, int limit = 50, CancellationToken cancellationToken = default)
    {
        var packet = new MessageHistoryRequestPacket { ChatId = chatId, Limit = limit };
        var response = (MessageHistoryResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
        return response.Messages;
    }
}
