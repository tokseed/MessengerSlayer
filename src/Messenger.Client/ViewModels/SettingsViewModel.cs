using System.Net.Mail;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Authentication;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Profiles;

namespace Messenger.Client.ViewModels;

public sealed partial class SettingsViewModel :
    ViewModelBase,
    IDisposable
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IFilePickerService _filePickerService;
    private readonly IClientProfileService _profileService;

    private string? _avatarDataUri;

    [ObservableProperty]
    private string _firstName =
        string.Empty;

    [ObservableProperty]
    private string _lastName =
        string.Empty;

    [ObservableProperty]
    private string _phoneNumber =
        string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private string _email =
        string.Empty;

    [ObservableProperty]
    private Bitmap? _avatarImage;

    [ObservableProperty]
    private bool _hasAvatar;

    [ObservableProperty]
    private string _profileStatusText =
        "Сервер коллег не изменяется: email и аватар редактируются локально.";

    [ObservableProperty]
    private bool _notificationsEnabled =
        true;

    [ObservableProperty]
    private bool _launchOnStartup;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SaveProfileCommand))]
    private bool _isBusy;

    public SettingsViewModel(
        string username,
        IAuthenticationService authenticationService,
        IFilePickerService filePickerService,
        IClientProfileService profileService)
    {
        Username =
            username;

        _authenticationService =
            authenticationService ??
            throw new ArgumentNullException(
                nameof(authenticationService));

        _filePickerService =
            filePickerService ??
            throw new ArgumentNullException(
                nameof(filePickerService));

        _profileService =
            profileService ??
            throw new ArgumentNullException(
                nameof(profileService));

        _ =
            LoadProfileAsync();
    }

    public event EventHandler?
        BackRequested;

    public event EventHandler?
        LogoutCompleted;

    public string Username { get; }

    public bool HasNoAvatar =>
        !HasAvatar;

    public string DisplayName
    {
        get
        {
            string fullName =
                $"{FirstName} {LastName}".Trim();

            return string.IsNullOrWhiteSpace(
                    fullName)
                ? Username
                : fullName;
        }
    }

    partial void OnFirstNameChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(DisplayName));
    }

    partial void OnLastNameChanged(
        string value)
    {
        OnPropertyChanged(
            nameof(DisplayName));
    }

    partial void OnHasAvatarChanged(
        bool value)
    {
        OnPropertyChanged(
            nameof(HasNoAvatar));
    }

    private bool CanSaveProfile()
    {
        return !IsBusy &&
               MailAddress.TryCreate(
                   Email.Trim(),
                   out _);
    }

    [RelayCommand]
    private void Back()
    {
        BackRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    [RelayCommand]
    private async Task ChooseAvatarAsync()
    {
        PickedFile? file =
            await _filePickerService.PickAvatarAsync(
                CancellationToken.None);

        if (file == null)
        {
            return;
        }

        if (!AvatarCodec.TryEncode(
                file,
                out string dataUri,
                out string error))
        {
            ProfileStatusText =
                error;

            return;
        }

        SetAvatar(
            dataUri);

        ProfileStatusText =
            "Нажмите «Сохранить профиль». Изменение останется только в клиенте.";
    }

    [RelayCommand]
    private void RemoveAvatar()
    {
        SetAvatar(
            null);

        ProfileStatusText =
            "Нажмите «Сохранить профиль». Удаление останется только в клиенте.";
    }

    [RelayCommand(CanExecute = nameof(CanSaveProfile))]
    private async Task SaveProfileAsync()
    {
        if (!MailAddress.TryCreate(
                Email.Trim(),
                out _))
        {
            ProfileStatusText =
                "Введите корректный email.";

            return;
        }

        IsBusy =
            true;

        try
        {
            await _profileService.SaveLocalOverridesAsync(
                Email,
                _avatarDataUri,
                CancellationToken.None);

            ProfileStatusText =
                "Сохранено локально. Для записи в БД нужен серверный Profile API от коллеги.";
        }
        catch
        {
            ProfileStatusText =
                "Не удалось сохранить локальные настройки профиля.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy =
            true;

        try
        {
            await _authenticationService.LogoutAsync(
                CancellationToken.None);

            LogoutCompleted?.Invoke(
                this,
                EventArgs.Empty);
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private async Task LoadProfileAsync()
    {
        IsBusy =
            true;

        try
        {
            ClientProfileSnapshot profile =
                await _profileService.GetProfileAsync(
                    CancellationToken.None);

            FirstName =
                profile.FirstName;

            LastName =
                profile.LastName;

            PhoneNumber =
                profile.PhoneNumber;

            Email =
                profile.Email;

            SetAvatar(
                profile.AvatarDataUri);

            ProfileStatusText =
                profile.EmailIsLocalOverride ||
                profile.AvatarIsLocalOverride
                    ? "Показаны локальные изменения поверх данных сервера."
                    : "Данные прочитаны через существующий UserList API. Изменения сохраняются локально.";
        }
        catch
        {
            ClientProfileSnapshot local =
                _profileService.GetLocalSnapshot(
                    Username);

            Email =
                local.Email;

            SetAvatar(
                local.AvatarDataUri);

            ProfileStatusText =
                "Серверный профиль прочитать не удалось; показаны локальные данные.";
        }
        finally
        {
            IsBusy =
                false;
        }
    }

    private void SetAvatar(
        string? avatarDataUri)
    {
        AvatarImage?.Dispose();

        _avatarDataUri =
            avatarDataUri;

        AvatarImage =
            AvatarCodec.TryCreateBitmap(
                avatarDataUri);

        HasAvatar =
            AvatarImage != null;
    }

    public void Dispose()
    {
        AvatarImage?.Dispose();

        AvatarImage =
            null;
    }
}
