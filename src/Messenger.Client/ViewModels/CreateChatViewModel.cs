using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Chats;
using Messenger.Client.UIModels;
namespace Messenger.Client.ViewModels;
public sealed partial class CreateChatViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(CreateChatCommand))] private string _chatTitle = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(AddMemberCommand))] private string _memberUsername = string.Empty;
    [ObservableProperty] private CreateChatMemberItem? _selectedMember;
    [ObservableProperty] private string _statusText = string.Empty;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(CreateChatCommand))] private bool _isBusy;
    [ObservableProperty] private bool _hasMembers;
    [ObservableProperty] private bool _isMemberListEmpty = true;
    public CreateChatViewModel(IChatService chatService)
    {
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        Members = new ObservableCollection<CreateChatMemberItem>();
        Members.CollectionChanged += (_, _) => { HasMembers = Members.Count > 0; IsMemberListEmpty = Members.Count == 0; CreateChatCommand.NotifyCanExecuteChanged(); };
    }
    public event EventHandler? BackRequested;
    public event EventHandler? ChatCreated;
    public ObservableCollection<CreateChatMemberItem> Members { get; }
    private bool CanAddMember() => !string.IsNullOrWhiteSpace(MemberUsername);
    [RelayCommand(CanExecute = nameof(CanAddMember))]
    private void AddMember()
    {
        string username = MemberUsername.Trim();
        bool exists = Members.Any(item => string.Equals(item.Username, username, StringComparison.OrdinalIgnoreCase));
        if (exists) { StatusText = "Пользователь уже добавлен."; return; }
        Members.Add(new CreateChatMemberItem { Username = username });
        MemberUsername = string.Empty; StatusText = string.Empty;
    }
    [RelayCommand]
    private void RemoveSelectedMember()
    {
        if (SelectedMember == null) return;
        Members.Remove(SelectedMember); SelectedMember = null;
    }
    private bool CanCreateChat() => !IsBusy && Members.Count > 0;
    [RelayCommand(CanExecute = nameof(CanCreateChat))]
    private async Task CreateChatAsync()
    {
        IsBusy = true; StatusText = string.Empty;
        try
        {
            IReadOnlyList<string> usernames = Members.Select(item => item.Username).ToArray();
            bool created = await _chatService.CreateChatAsync(ChatTitle.Trim(), usernames, CancellationToken.None);
            if (!created) { StatusText = "Не удалось создать чат. Проверьте имя участника и соединение с сервером."; return; }
            ChatCreated?.Invoke(this, EventArgs.Empty);
        }
        finally { IsBusy = false; }
    }
    [RelayCommand] private void Back() => BackRequested?.Invoke(this, EventArgs.Empty);
}
