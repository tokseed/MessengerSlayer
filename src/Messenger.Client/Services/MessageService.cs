using Messenger.Client.Network;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services;

public sealed class MessageService
{
    private readonly TcpClientService _client;

    public MessageService(TcpClientService client)
    {
        _client = client;
    }

    public event Action<MessageDto>? OnMessageReceived;

    public void RegisterMessageHandler()
    {
        _client.OnPacketReceived += packet =>
        {
            if (packet is MessagePacket message)
            {
                var dto = new MessageDto
                {
                    Id = 0,
                    ChatId = message.ChatId,
                    SenderId = message.SenderId,
                    Content = message.Content,
                    SentAt = DateTime.UtcNow
                };
                OnMessageReceived?.Invoke(dto);
            }
        };
    }

    public async Task<MessageAckPacket> SendAsync(int chatId, string content, int? replyToMessageId = null, CancellationToken cancellationToken = default)
    {
        var packet = new MessagePacket
        {
            ChatId = chatId,
            Content = content,
            ReplyToMessageId = replyToMessageId
        };

        return (MessageAckPacket)await _client.SendAndWaitAsync(packet, cancellationToken);
    }
}
