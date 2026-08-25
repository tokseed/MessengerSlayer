using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class MessageHistoryResponsePacket : Packet
{
    public override PacketType Type => PacketType.MessageHistoryResponse;
    public List<MessageDto> Messages { get; init; } = [];
}
