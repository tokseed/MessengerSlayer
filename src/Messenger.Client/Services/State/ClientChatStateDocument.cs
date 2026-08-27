namespace Messenger.Client.Services.State;

public sealed class ClientChatStateDocument
{
    public HashSet<long> HiddenChatIds { get; init; } =
        new();

    public Dictionary<long, DateTime> LastReadUtc { get; init; } =
        new();

    public Dictionary<long, DateTime> ClearedBeforeUtc { get; init; } =
        new();
}
