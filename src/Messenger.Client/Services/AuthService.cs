using Messenger.Client.Network;
using Messenger.Shared.Models;
using Messenger.Shared.Packets;

namespace Messenger.Client.Services;

public sealed class AuthService
{
    private readonly TcpClientService _client;

    public AuthService(TcpClientService client)
    {
        _client = client;
    }

    public async Task<AuthResponsePacket> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var packet = new AuthPacket
        {
            Username = username,
            PasswordHash = password
        };

        return (AuthResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
    }

    public async Task<RegisterResponsePacket> RegisterAsync(
        string username, string password,
        string firstName, string lastName, string phoneNumber,
        CancellationToken cancellationToken = default)
    {
        var packet = new RegisterPacket
        {
            Username = username,
            PasswordHash = password,
            FirstName = firstName,
            LastName = lastName,
            PhoneNumber = phoneNumber
        };

        return (RegisterResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
    }

    public async Task<List<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var packet = new UserListRequestPacket();
        var response = (UserListResponsePacket)await _client.SendAndWaitAsync(packet, cancellationToken);
        return response.Users;
    }
}
