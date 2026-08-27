using Messenger.Shared.Models;

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
        string email,
        string? avatarUrl,
        string password,
        CancellationToken cancellationToken);

    Task<UserDto?> GetProfileAsync(
        CancellationToken cancellationToken);

    Task<UserDto?> UpdateProfileAsync(
        string email,
        string? avatarUrl,
        CancellationToken cancellationToken);

    Task LogoutAsync(
        CancellationToken cancellationToken);
}
