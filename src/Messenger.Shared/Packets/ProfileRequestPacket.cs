namespace Messenger.Shared.Packets;

public sealed class ProfileRequestPacket : Packet
{
    public override PacketType Type =>
        PacketType.ProfileRequest;
}
