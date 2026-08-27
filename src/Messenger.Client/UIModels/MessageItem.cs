namespace Messenger.Client.UIModels;

public sealed class MessageItem
{
    public long MessageId { get; init; }

    public long ChatId { get; init; }

    public int SenderId { get; init; }

    public string Text { get; init; } =
        string.Empty;

    public DateTime SentAtUtc { get; init; }

    public string TimeText =>
        SentAtUtc
            .ToLocalTime()
            .ToString("HH:mm");

    public bool IsOwn { get; init; }

    public string StatusText { get; init; } =
        string.Empty;

    public bool IsFile { get; init; }

    public bool IsTextMessage =>
        !IsFile;

    public string FileName { get; init; } =
        string.Empty;

    public string FileSizeText { get; init; } =
        string.Empty;

    public byte[] FileData { get; init; } =
        Array.Empty<byte>();

    public string SearchContent =>
        IsFile
            ? FileName
            : Text;
}
