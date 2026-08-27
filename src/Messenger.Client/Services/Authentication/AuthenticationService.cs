using Messenger.Client.Services.Network;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services.Authentication;

public sealed class AuthenticationService :
    IAuthenticationService
{
    private readonly IMessengerConnection _connection;
    private readonly ClientSessionState _session;

    public AuthenticationService(
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
    }

    public async Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        await EnsureConnectedAsync(
            cancellationToken);

        AuthResponsePacket response =
            await _connection.SendRequestAsync<AuthPacket, AuthResponsePacket>(
                new AuthPacket
                {
                    Username =
                        username.Trim(),
                    PasswordHash =
                        password
                },
                cancellationToken);

        if (!response.Success)
        {
            return false;
        }

        _session.SetAuthenticated(
            response.UserId,
            username.Trim());

        UserDto? profile =
            await GetProfileAsync(
                cancellationToken);

        return profile != null;
    }

    public async Task<bool> RegisterAsync(
        string username,
        string firstName,
        string lastName,
        string phoneNumber,
        string email,
        string? avatarUrl,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
            string.IsNullOrWhiteSpace(email) ||
            string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        await EnsureConnectedAsync(
            cancellationToken);

        RegisterResponsePacket response =
            await _connection.SendRequestAsync<RegisterPacket, RegisterResponsePacket>(
                new RegisterPacket
                {
                    Username =
                        username.Trim(),
                    PasswordHash =
                        password,
                    FirstName =
                        firstName.Trim(),
                    LastName =
                        lastName.Trim(),
                    PhoneNumber =
                        phoneNumber.Trim(),
                    Email =
                        email.Trim(),
                    AvatarUrl =
                        avatarUrl
                },
                cancellationToken);

        if (!response.Success)
        {
            return false;
        }

        // Registration on the team server creates the account but does not
        // authenticate the socket, therefore perform a normal login.
        return await LoginAsync(
            username,
            password,
            cancellationToken);
    }

    public async Task<UserDto?> GetProfileAsync(
        CancellationToken cancellationToken)
    {
        if (!_session.IsAuthenticated)
        {
            return null;
        }

        await EnsureConnectedAsync(
            cancellationToken);

        ProfileResponsePacket response =
            await _connection.SendRequestAsync<ProfileRequestPacket, ProfileResponsePacket>(
                new ProfileRequestPacket(),
                cancellationToken);

        if (!response.Success ||
            response.User == null)
        {
            return null;
        }

        _session.SetProfile(
            response.User);

        return response.User;
    }

    public async Task<UserDto?> UpdateProfileAsync(
        string email,
        string? avatarUrl,
        CancellationToken cancellationToken)
    {
        if (!_session.IsAuthenticated ||
            string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        await EnsureConnectedAsync(
            cancellationToken);

        ProfileUpdateResponsePacket response =
            await _connection.SendRequestAsync<ProfileUpdatePacket, ProfileUpdateResponsePacket>(
                new ProfileUpdatePacket
                {
                    Email =
                        email.Trim(),
                    AvatarUrl =
                        avatarUrl
                },
                cancellationToken);

        if (!response.Success ||
            response.User == null)
        {
            return null;
        }

        _session.SetProfile(
            response.User);

        return response.User;
    }

    public async Task LogoutAsync(
        CancellationToken cancellationToken)
    {
        _session.Clear();

        await _connection.DisconnectAsync(
            cancellationToken);
    }

    private async Task EnsureConnectedAsync(
        CancellationToken cancellationToken)
    {
        if (!_connection.IsConnected)
        {
            await _connection.ConnectAsync(
                cancellationToken);
        }
    }
}
