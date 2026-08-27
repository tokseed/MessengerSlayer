using Messenger.Shared.Models;

namespace Messenger.Shared.Packets;

public sealed class ProfileUpdateResponsePacket : Packet
{
    public override PacketType Type =>
        PacketType.ProfileUpdateResponse;

    public bool Success { get; init; }

    public UserDto? User { get; init; }

    public string Error { get; init; } =
        string.Empty;
}
