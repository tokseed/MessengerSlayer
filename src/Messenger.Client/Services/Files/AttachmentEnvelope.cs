namespace Messenger.Client.Services.Files;

public sealed class AttachmentEnvelope
{
    public string FileName { get; init; } =
        string.Empty;

    public string DataBase64 { get; init; } =
        string.Empty;
}
