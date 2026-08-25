namespace Messenger.Shared.Packets;

public sealed class RegisterPacket : Packet
{
    public override PacketType Type => PacketType.Register;
    public string Username { get; init; } = string.Empty;
    public string PasswordHash { get; init; } = string.Empty;
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
}
