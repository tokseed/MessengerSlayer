using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Threading;
using Messenger.Client.UIModels;
namespace Messenger.Client.ViewModels;
public sealed partial class ChatsViewModel : ViewModelBase
{
    private readonly IChatService _chatService;
    private readonly IUiDispatcher _uiDispatcher;
    [ObservableProperty] private ChatListItem? _selectedChat;
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty][NotifyCanExecuteChangedFor(nameof(SendMessageCommand))] private string _messageText = string.Empty;
    [ObservableProperty] private string _searchText = string.Empty;
    [ObservableProperty] private bool _hasChats;
    [ObservableProperty] private bool _isChatListEmpty = true;
    [ObservableProperty] private bool _hasSelectedChat;
    [ObservableProperty] private bool _hasNoSelectedChat = true;
    public ChatsViewModel(string username, IChatService chatService, IUiDispatcher uiDispatcher)
    {
        Username = username;
        _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
        _uiDispatcher = uiDispatcher ?? throw new ArgumentNullException(nameof(uiDispatcher));
        Chats = new ObservableCollection<ChatListItem>();
        Messages = new ObservableCollection<MessageItem>();
        _ = LoadChatsAsync();
    }
    public event EventHandler? CreateChatRequested;
    public event EventHandler? SettingsRequested;
    public string Username { get; }
    public ObservableCollection<ChatListItem> Chats { get; }
    public ObservableCollection<MessageItem> Messages { get; }
    partial void OnSelectedChatChanged(ChatListItem? value)
    {
        HasSelectedChat = value != null;
        HasNoSelectedChat = value == null;
        SendMessageCommand.NotifyCanExecuteChanged();
        if (value == null) { Messages.Clear(); return; }
        _ = LoadMessagesAsync(value.ChatId);
    }
    [RelayCommand]
    private async Task LoadChatsAsync()
    {
        IsLoading = true;
        try { ApplyChats(await _chatService.GetChatsAsync(CancellationToken.None)); }
        finally { IsLoading = false; }
    }
    private void ApplyChats(IReadOnlyList<ChatListItem> chats)
    {
        void Apply()
        {
            Chats.Clear();
            foreach (ChatListItem chat in chats) Chats.Add(chat);
            HasChats = Chats.Count > 0;
            IsChatListEmpty = Chats.Count == 0;
            SelectedChat = Chats.Count > 0 ? Chats[0] : null;
        }
        if (_uiDispatcher.CheckAccess()) Apply(); else _uiDispatcher.Post(Apply);
    }
    private async Task LoadMessagesAsync(long chatId)
    {
        IReadOnlyList<MessageItem> messages = await _chatService.GetMessagesAsync(chatId, CancellationToken.None);
        void Apply()
        {
            Messages.Clear();
            foreach (MessageItem message in messages) Messages.Add(message);
        }
        if (_uiDispatcher.CheckAccess()) Apply(); else _uiDispatcher.Post(Apply);
    }
    private bool CanSendMessage() => SelectedChat != null && !string.IsNullOrWhiteSpace(MessageText);
    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (SelectedChat == null) return;
        string text = MessageText.Trim();
        await _chatService.SendMessageAsync(SelectedChat.ChatId, text, CancellationToken.None);
        MessageText = string.Empty;
    }
    [RelayCommand] private void CreateChat() => CreateChatRequested?.Invoke(this, EventArgs.Empty);
    [RelayCommand] private void OpenSettings() => SettingsRequested?.Invoke(this, EventArgs.Empty);
}
