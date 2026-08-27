using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using Messenger.Client.Services.Profiles;

namespace Messenger.Client.UIModels;

public sealed class ChatListItem :
    ObservableObject,
    IDisposable
{
    private string _title =
        string.Empty;

    private string _lastMessage =
        string.Empty;

    private string _timeText =
        string.Empty;

    private string _initials =
        string.Empty;

    private string? _avatarUrl;

    private Bitmap? _avatarImage;

    private int _unreadCount;

    private bool _isOnline;

    private DateTime? _lastActivityUtc;

    public long ChatId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public string Title
    {
        get => _title;
        set => SetProperty(
            ref _title,
            value);
    }

    public string LastMessage
    {
        get => _lastMessage;
        set => SetProperty(
            ref _lastMessage,
            value);
    }

    public string TimeText
    {
        get => _timeText;
        set => SetProperty(
            ref _timeText,
            value);
    }

    public string Initials
    {
        get => _initials;
        set => SetProperty(
            ref _initials,
            value);
    }

    public string? AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            if (!SetProperty(
                    ref _avatarUrl,
                    value))
            {
                return;
            }

            AvatarImage?.Dispose();

            AvatarImage =
                AvatarCodec.TryCreateBitmap(
                    value);
        }
    }

    public Bitmap? AvatarImage
    {
        get => _avatarImage;
        private set
        {
            if (!SetProperty(
                    ref _avatarImage,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasAvatar));

            OnPropertyChanged(
                nameof(HasNoAvatar));
        }
    }

    public bool HasAvatar =>
        AvatarImage != null;

    public bool HasNoAvatar =>
        !HasAvatar;

    public int UnreadCount
    {
        get => _unreadCount;
        set
        {
            if (!SetProperty(
                    ref _unreadCount,
                    value))
            {
                return;
            }

            OnPropertyChanged(
                nameof(HasUnreadMessages));

            OnPropertyChanged(
                nameof(UnreadText));
        }
    }

    public bool HasUnreadMessages =>
        UnreadCount > 0;

    public string UnreadText =>
        UnreadCount > 99
            ? "99+"
            : UnreadCount.ToString();

    public bool IsOnline
    {
        get => _isOnline;
        set => SetProperty(
            ref _isOnline,
            value);
    }

    public DateTime? LastActivityUtc
    {
        get => _lastActivityUtc;
        set => SetProperty(
            ref _lastActivityUtc,
            value);
    }

    public void ApplyServerMetadata(
        ChatListItem source)
    {
        ArgumentNullException.ThrowIfNull(
            source);

        Title =
            source.Title;

        Initials =
            source.Initials;

        AvatarUrl =
            source.AvatarUrl;

        IsOnline =
            source.IsOnline;
    }

    public void ApplyPreview(
        MessageItem? message)
    {
        if (message == null)
        {
            LastMessage =
                string.Empty;

            TimeText =
                string.Empty;

            LastActivityUtc =
                CreatedAtUtc;

            return;
        }

        LastMessage =
            message.IsFile
                ? $"Файл: {message.FileName}"
                : message.Text;

        TimeText =
            message.TimeText;

        LastActivityUtc =
            message.SentAtUtc;
    }

    public void Dispose()
    {
        AvatarImage?.Dispose();

        AvatarImage =
            null;
    }
}
