namespace Messenger.Client.Services.State;

public interface IClientChatStateStore
{
    void SetCurrentUser(
        string username);

    Task<IReadOnlySet<long>> GetHiddenChatIdsAsync(
        CancellationToken cancellationToken);

    Task<DateTime?> GetLastReadUtcAsync(
        long chatId,
        CancellationToken cancellationToken);

    Task<DateTime?> GetClearedBeforeUtcAsync(
        long chatId,
        CancellationToken cancellationToken);

    Task MarkReadAsync(
        long chatId,
        DateTime readAtUtc,
        CancellationToken cancellationToken);

    Task ClearChatAsync(
        long chatId,
        DateTime clearedAtUtc,
        CancellationToken cancellationToken);

    Task HideChatAsync(
        long chatId,
        CancellationToken cancellationToken);

    Task UnhideChatAsync(
        long chatId,
        CancellationToken cancellationToken);
}
