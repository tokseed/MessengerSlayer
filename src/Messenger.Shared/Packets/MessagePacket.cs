namespace Messenger.Shared.Packets;

public sealed class MessagePacket : Packet
{
    public override PacketType Type => PacketType.Message;
    public int SenderId { get; init; }
    public int ChatId { get; init; }
    public string Content { get; init; } = string.Empty;
    public int? ReplyToMessageId { get; init; }
}
