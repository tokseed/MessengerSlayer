namespace Messenger.Client.Services.Authentication;

public sealed class ClientSessionState
{
    public int? UserId { get; private set; }

    public string Username { get; private set; } =
        string.Empty;

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

    public void Clear()
    {
        UserId =
            null;

        Username =
            string.Empty;
    }
}
