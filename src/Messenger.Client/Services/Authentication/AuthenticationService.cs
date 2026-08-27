using Messenger.Client.Services.Network;
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

        return true;
    }

    public async Task<bool> RegisterAsync(
        string username,
        string firstName,
        string lastName,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) ||
            string.IsNullOrWhiteSpace(firstName) ||
            string.IsNullOrWhiteSpace(lastName) ||
            string.IsNullOrWhiteSpace(phoneNumber) ||
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
                        phoneNumber.Trim()
                },
                cancellationToken);

        if (!response.Success)
        {
            return false;
        }

        // Team server registration does not authenticate the current session,
        // so immediately perform a normal login on the same TLS connection.
        return await LoginAsync(
            username,
            password,
            cancellationToken);
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
