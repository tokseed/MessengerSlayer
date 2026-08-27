namespace Messenger.Client.UIModels;
public sealed class CreateChatMemberItem
{
    public string Username { get; init; } = string.Empty;
    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Username)) return string.Empty;
            return Username.Trim().Substring(0, 1).ToUpperInvariant();
        }
    }
}
