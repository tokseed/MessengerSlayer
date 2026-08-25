namespace Messenger.Shared.Packets;

public sealed class MessageHistoryRequestPacket : Packet
{
    public override PacketType Type => PacketType.MessageHistoryRequest;
    public int ChatId { get; init; }
    public int Limit { get; init; } = 50;
}
