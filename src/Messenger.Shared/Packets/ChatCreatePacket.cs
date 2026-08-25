namespace Messenger.Shared.Packets;

public sealed class ChatCreatePacket : Packet
{
    public override PacketType Type => PacketType.ChatCreate;
    public string Title { get; init; } = string.Empty;
    public string ChatType { get; init; } = string.Empty;
    public List<int> ParticipantIds { get; init; } = [];
}
