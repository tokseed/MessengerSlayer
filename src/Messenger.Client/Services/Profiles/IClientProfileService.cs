namespace Messenger.Client.Services.Profiles;

public interface IClientProfileService
{
    ClientProfileSnapshot GetLocalSnapshot(
        string username);

    Task<ClientProfileSnapshot> GetProfileAsync(
        CancellationToken cancellationToken);

    Task SaveLocalOverridesAsync(
        string email,
        string? avatarDataUri,
        CancellationToken cancellationToken);
}
