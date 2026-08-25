namespace Messenger.Shared.Packets;

public abstract class Packet
{
    public abstract PacketType Type { get; }
}
