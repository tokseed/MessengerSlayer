namespace Messenger.Client.Services.Authentication;

public interface IAuthenticationService
{
    Task<bool> LoginAsync(
        string username,
        string password,
        CancellationToken cancellationToken);

    Task<bool> RegisterAsync(
        string username,
        string firstName,
        string lastName,
        string phoneNumber,
        string password,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        CancellationToken cancellationToken);
}
