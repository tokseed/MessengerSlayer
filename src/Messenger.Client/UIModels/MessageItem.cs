namespace Messenger.Client.UIModels;

public sealed class MessageItem
{
    public long MessageId { get; init; }

    public string Text { get; init; } =
        string.Empty;

    public string TimeText { get; init; } =
        string.Empty;

    public bool IsOwn { get; init; }

    public string StatusText { get; init; } =
        string.Empty;
}
