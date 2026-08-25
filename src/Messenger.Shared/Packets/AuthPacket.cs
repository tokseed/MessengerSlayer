namespace Messenger.Shared.Packets;

public sealed class AuthPacket : Packet
{
    public override PacketType Type => PacketType.Auth;
    public string Username { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
}
