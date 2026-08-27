namespace Messenger.Client.Services.Profiles;

public sealed class LocalProfileDocument
{
    public Dictionary<string, LocalProfileEntry> Profiles { get; set; } =
        new(
            StringComparer.OrdinalIgnoreCase);
}

public sealed class LocalProfileEntry
{
    public bool HasEmailOverride { get; set; }

    public string Email { get; set; } =
        string.Empty;

    public bool HasAvatarOverride { get; set; }

    public string? AvatarDataUri { get; set; }
}
