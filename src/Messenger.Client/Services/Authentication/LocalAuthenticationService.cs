using System;
using System.Threading;
using System.Threading.Tasks;
using Messenger.Client.Services.Network;
namespace Messenger.Client.Services.Authentication;
public sealed class LocalAuthenticationService : IAuthenticationService
{
    private readonly IMessengerConnection _messengerConnection;
    public LocalAuthenticationService(IMessengerConnection messengerConnection)
    {
        _messengerConnection = messengerConnection ?? throw new ArgumentNullException(nameof(messengerConnection));
    }
    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password)) return false;
        if (!_messengerConnection.IsConnected) await _messengerConnection.ConnectAsync(cancellationToken);
        return true;
    }
    public async Task<bool> RegisterAsync(string username, string displayName, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(displayName) || string.IsNullOrWhiteSpace(password)) return false;
        if (!_messengerConnection.IsConnected) await _messengerConnection.ConnectAsync(cancellationToken);
        return true;
    }
    public Task LogoutAsync(CancellationToken cancellationToken) => _messengerConnection.DisconnectAsync(cancellationToken);
}
