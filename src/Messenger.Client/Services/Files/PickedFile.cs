namespace Messenger.Client.Services.Files;

public sealed class PickedFile
{
    public string Name { get; init; } =
        string.Empty;

    public byte[] Data { get; init; } =
        Array.Empty<byte>();
}
