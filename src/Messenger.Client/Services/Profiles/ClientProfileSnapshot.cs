namespace Messenger.Client.Services.Profiles;

public sealed class ClientProfileSnapshot
{
    public string Username { get; init; } =
        string.Empty;

    public string FirstName { get; init; } =
        string.Empty;

    public string LastName { get; init; } =
        string.Empty;

    public string PhoneNumber { get; init; } =
        string.Empty;

    public string Email { get; init; } =
        string.Empty;

    public string? AvatarDataUri { get; init; }

    public bool EmailIsLocalOverride { get; init; }

    public bool AvatarIsLocalOverride { get; init; }
}
