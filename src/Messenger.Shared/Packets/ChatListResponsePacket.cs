using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class ChatListResponsePacket : Packet
{
    public override PacketType Type => PacketType.ChatListResponse;
    public List<ChatDto> Chats { get; init; } = [];
}
