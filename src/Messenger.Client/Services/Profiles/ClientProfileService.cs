using System.Text.Json;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Network;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services.Profiles;

public sealed class ClientProfileService :
    IClientProfileService
{
    private readonly IMessengerConnection _connection;
    private readonly ClientSessionState _session;
    private readonly string _storePath;
    private readonly SemaphoreSlim _storeLock =
        new(1, 1);

    public ClientProfileService(
        IMessengerConnection connection,
        ClientSessionState session)
    {
        _connection =
            connection ??
            throw new ArgumentNullException(
                nameof(connection));

        _session =
            session ??
            throw new ArgumentNullException(
                nameof(session));

        string directory =
            Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.LocalApplicationData),
                "MessengerSlayer");

        Directory.CreateDirectory(
            directory);

        _storePath =
            Path.Combine(
                directory,
                "client-profiles.json");
    }

    public ClientProfileSnapshot GetLocalSnapshot(
        string username)
    {
        LocalProfileDocument document =
            LoadDocument();

        document.Profiles.TryGetValue(
            username,
            out LocalProfileEntry? local);

        return new ClientProfileSnapshot
        {
            Username =
                username,
            Email =
                local?.Email ??
                string.Empty,
            AvatarDataUri =
                local?.AvatarDataUri,
            EmailIsLocalOverride =
                local?.HasEmailOverride ??
                false,
            AvatarIsLocalOverride =
                local?.HasAvatarOverride ??
                false
        };
    }

    public async Task<ClientProfileSnapshot> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        string username =
            _session.Username;

        ClientProfileSnapshot local =
            GetLocalSnapshot(
                username);

        UserDto? serverUser =
            null;

        if (_session.IsAuthenticated &&
            _connection.IsConnected)
        {
            try
            {
                UserListResponsePacket response =
                    await _connection.SendRequestAsync<UserListRequestPacket, UserListResponsePacket>(
                        new UserListRequestPacket(),
                        cancellationToken);

                serverUser =
                    response.Users.FirstOrDefault(
                        user =>
                            user.Id ==
                            _session.UserId);
            }
            catch
            {
                // Server profile read is best-effort. The colleague protocol
                // has no dedicated profile packet, so local UI state remains usable.
            }
        }

        return new ClientProfileSnapshot
        {
            Username =
                serverUser?.Username ??
                username,
            FirstName =
                serverUser?.FirstName ??
                string.Empty,
            LastName =
                serverUser?.LastName ??
                string.Empty,
            PhoneNumber =
                serverUser?.PhoneNumber ??
                string.Empty,
            Email =
                local.EmailIsLocalOverride
                    ? local.Email
                    : serverUser?.Email ??
                      string.Empty,
            AvatarDataUri =
                local.AvatarIsLocalOverride
                    ? local.AvatarDataUri
                    : serverUser?.AvatarUrl,
            EmailIsLocalOverride =
                local.EmailIsLocalOverride,
            AvatarIsLocalOverride =
                local.AvatarIsLocalOverride
        };
    }

    public async Task SaveLocalOverridesAsync(
        string email,
        string? avatarDataUri,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                _session.Username))
        {
            throw new InvalidOperationException(
                "No authenticated user.");
        }

        await _storeLock.WaitAsync(
            cancellationToken);

        try
        {
            LocalProfileDocument document =
                LoadDocument();

            document.Profiles[
                _session.Username] =
                new LocalProfileEntry
                {
                    HasEmailOverride =
                        true,
                    Email =
                        email.Trim(),
                    HasAvatarOverride =
                        true,
                    AvatarDataUri =
                        avatarDataUri
                };

            string json =
                JsonSerializer.Serialize(
                    document,
                    new JsonSerializerOptions
                    {
                        WriteIndented =
                            true
                    });

            await File.WriteAllTextAsync(
                _storePath,
                json,
                cancellationToken);
        }
        finally
        {
            _storeLock.Release();
        }
    }

    private LocalProfileDocument LoadDocument()
    {
        try
        {
            if (!File.Exists(
                    _storePath))
            {
                return new LocalProfileDocument();
            }

            string json =
                File.ReadAllText(
                    _storePath);

            return JsonSerializer.Deserialize<LocalProfileDocument>(
                       json) ??
                   new LocalProfileDocument();
        }
        catch
        {
            return new LocalProfileDocument();
        }
    }
}
