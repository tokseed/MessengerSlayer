namespace Messenger.Shared.Packets;

public sealed class AuthResponsePacket : Packet
{
    public override PacketType Type => PacketType.AuthResponse;
    public bool Success { get; init; }
    public int UserId { get; init; }
    public string Error { get; init; } = string.Empty;
}
