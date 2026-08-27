using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Messenger.Client.Services.State;

public sealed class JsonClientChatStateStore :
    IClientChatStateStore
{
    private readonly SemaphoreSlim _gate =
        new(1, 1);

    private readonly string _directory;

    private string? _filePath;
    private ClientChatStateDocument? _state;

    public JsonClientChatStateStore()
    {
        string root =
            Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);

        _directory =
            Path.Combine(
                root,
                "MessengerSlayer");

    }

    public void SetCurrentUser(
        string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException(
                "Username cannot be empty.",
                nameof(username));
        }

        byte[] usernameBytes =
            Encoding.UTF8.GetBytes(
                username.Trim().ToUpperInvariant());

        string scope =
            Convert.ToHexString(
                SHA256.HashData(usernameBytes))
            [..16];

        Directory.CreateDirectory(
            _directory);

        _filePath =
            Path.Combine(
                _directory,
                $"client-state-{scope}.json");

        _state =
            null;
    }

    public async Task<IReadOnlySet<long>> GetHiddenChatIdsAsync(
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            ClientChatStateDocument state =
                await GetStateAsync(
                    cancellationToken);

            return new HashSet<long>(
                state.HiddenChatIds);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DateTime?> GetLastReadUtcAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            ClientChatStateDocument state =
                await GetStateAsync(
                    cancellationToken);

            return state.LastReadUtc.TryGetValue(
                chatId,
                out DateTime value)
                ? value
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<DateTime?> GetClearedBeforeUtcAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            ClientChatStateDocument state =
                await GetStateAsync(
                    cancellationToken);

            return state.ClearedBeforeUtc.TryGetValue(
                chatId,
                out DateTime value)
                ? value
                : null;
        }
        finally
        {
            _gate.Release();
        }
    }

    public Task MarkReadAsync(
        long chatId,
        DateTime readAtUtc,
        CancellationToken cancellationToken)
    {
        return MutateAsync(
            state =>
            {
                DateTime normalized =
                    readAtUtc.ToUniversalTime();

                if (!state.LastReadUtc.TryGetValue(
                        chatId,
                        out DateTime current) ||
                    normalized > current)
                {
                    state.LastReadUtc[chatId] =
                        normalized;
                }
            },
            cancellationToken);
    }

    public Task ClearChatAsync(
        long chatId,
        DateTime clearedAtUtc,
        CancellationToken cancellationToken)
    {
        return MutateAsync(
            state =>
            {
                DateTime normalized =
                    clearedAtUtc.ToUniversalTime();

                state.ClearedBeforeUtc[chatId] =
                    normalized;

                state.LastReadUtc[chatId] =
                    normalized;
            },
            cancellationToken);
    }

    public Task HideChatAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        return MutateAsync(
            state =>
                state.HiddenChatIds.Add(
                    chatId),
            cancellationToken);
    }

    public Task UnhideChatAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        return MutateAsync(
            state =>
                state.HiddenChatIds.Remove(
                    chatId),
            cancellationToken);
    }

    private async Task MutateAsync(
        Action<ClientChatStateDocument> mutation,
        CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(
            cancellationToken);

        try
        {
            ClientChatStateDocument state =
                await GetStateAsync(
                    cancellationToken);

            mutation(
                state);

            await SaveAsync(
                state,
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task<ClientChatStateDocument> GetStateAsync(
        CancellationToken cancellationToken)
    {
        string filePath =
            GetFilePath();

        if (_state != null)
        {
            return _state;
        }

        if (!File.Exists(filePath))
        {
            _state =
                new ClientChatStateDocument();

            return _state;
        }

        await using FileStream stream =
            File.OpenRead(
                filePath);

        _state =
            await JsonSerializer.DeserializeAsync<ClientChatStateDocument>(
                stream,
                cancellationToken:
                    cancellationToken)
            ?? new ClientChatStateDocument();

        return _state;
    }

    private async Task SaveAsync(
        ClientChatStateDocument state,
        CancellationToken cancellationToken)
    {
        string filePath =
            GetFilePath();

        string tempPath =
            filePath +
            ".tmp";

        await using (FileStream stream =
                     File.Create(
                         tempPath))
        {
            await JsonSerializer.SerializeAsync(
                stream,
                state,
                new JsonSerializerOptions
                {
                    WriteIndented =
                        true
                },
                cancellationToken);
        }

        File.Move(
            tempPath,
            filePath,
            overwrite: true);
    }

    private string GetFilePath()
    {
        return _filePath
            ?? throw new InvalidOperationException(
                "Client chat state has no active user scope.");
    }
}
