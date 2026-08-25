using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class ChatListRequestPacket : Packet
{
    public override PacketType Type => PacketType.ChatListRequest;
    public int UserId { get; init; }
}
