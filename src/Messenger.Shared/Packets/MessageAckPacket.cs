namespace Messenger.Shared.Packets;

public sealed class MessageAckPacket : Packet
{
    public override PacketType Type => PacketType.MessageAck;
    public bool Success { get; init; }
    public int MessageId { get; init; }
    public string Error { get; init; } = string.Empty;
}
