using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class UserListResponsePacket : Packet
{
    public override PacketType Type => PacketType.UserListResponse;
    public List<UserDto> Users { get; init; } = [];
}
