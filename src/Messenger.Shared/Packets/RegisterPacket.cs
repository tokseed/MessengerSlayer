namespace Messenger.Shared.Packets;

public sealed class RegisterPacket : Packet
{
    public override PacketType Type =>
        PacketType.Register;

    public string Username { get; init; } =
        string.Empty;

    // The team contract historically named this PasswordHash, while the server
    // performs the actual hashing. TLS protects the plaintext in transit.
    public string PasswordHash { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public string PhoneNumber { get; init; } =
        string.Empty;

    public string Email { get; init; } =
        string.Empty;

    public string? AvatarUrl { get; init; }
}
