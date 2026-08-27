namespace Messenger.Shared.Packets;

public sealed class ProfileUpdatePacket : Packet
{
    public override PacketType Type =>
        PacketType.ProfileUpdate;

    public string Email { get; init; } =
        string.Empty;

    public string? AvatarUrl { get; init; }
}
