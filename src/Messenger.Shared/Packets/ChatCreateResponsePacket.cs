namespace Messenger.Shared.Packets;

public sealed class ChatCreateResponsePacket : Packet
{
    public override PacketType Type => PacketType.ChatCreateResponse;
    public bool Success { get; init; }
    public int ChatId { get; init; }
    public string Error { get; init; } = string.Empty;
}
