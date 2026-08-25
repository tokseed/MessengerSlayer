namespace Messenger.Shared.Packets;

public sealed class UserListRequestPacket : Packet
{
    public override PacketType Type => PacketType.UserListRequest;
}
