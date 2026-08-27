using Messenger.Shared.Models;

namespace Messenger.Client.Services.Authentication;

public sealed class ClientSessionState
{
    public int? UserId { get; private set; }

    public string Username { get; private set; } =
        string.Empty;

    public UserDto? Profile { get; private set; }

    public string? Email =>
        Profile?.Email;

    public string? AvatarUrl =>
        Profile?.AvatarUrl;

    public bool IsAuthenticated =>
        UserId.HasValue;

    public void SetAuthenticated(
        int userId,
        string username)
    {
        UserId =
            userId;

        Username =
            username;
    }

    public void SetProfile(
        UserDto profile)
    {
        ArgumentNullException.ThrowIfNull(
            profile);

        Profile =
            profile;

        UserId =
            profile.Id;

        Username =
            profile.Username;
    }

    public void Clear()
    {
        UserId =
            null;

        Username =
            string.Empty;

        Profile =
            null;
    }
}
