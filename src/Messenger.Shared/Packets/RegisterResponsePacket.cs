namespace Messenger.Shared.Packets;

public sealed class RegisterResponsePacket : Packet
{
    public override PacketType Type => PacketType.RegisterResponse;
    public bool Success { get; init; }
    public int UserId { get; init; }
    public string Error { get; init; } = string.Empty;
}
