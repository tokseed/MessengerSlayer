using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class ProfileResponsePacket : Packet
{
    public override PacketType Type =>
        PacketType.ProfileResponse;

    public bool Success { get; init; }

    public UserDto? User { get; init; }

    public string Error { get; init; } =
        string.Empty;
}
