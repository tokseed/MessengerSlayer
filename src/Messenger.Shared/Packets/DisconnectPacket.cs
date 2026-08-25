namespace Messenger.Shared.Packets;

public sealed class DisconnectPacket : Packet
{
    public override PacketType Type => PacketType.Disconnect;
}
