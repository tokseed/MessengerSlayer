using Avalonia.Media.Imaging;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Messenger.Client.Configuration;
using Messenger.Client.Services.Chats;
using Messenger.Client.Services.Files;
using Messenger.Client.Services.Profiles;
using Messenger.Client.Services.State;
using Messenger.Client.Services.Threading;
using Messenger.Client.UIModels;

namespace Messenger.Client.ViewModels;

public sealed partial class ChatsViewModel :
    ViewModelBase,
    IDisposable
{
    private readonly IChatService _chatService;
    private readonly IUiDispatcher _uiDispatcher;
    private readonly IFilePickerService _filePickerService;
    private readonly IClientChatStateStore _stateStore;
    private readonly TimeSpan _syncInterval;

    private readonly CancellationTokenSource _lifetimeCancellation =
        new();

    private readonly SemaphoreSlim _refreshLock =
        new(1, 1);

    private readonly List<ChatListItem> _allChats =
        new();

    private readonly List<MessageItem> _allMessages =
        new();

    private long? _loadedChatId;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(AttachFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearChatCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteChatCommand))]
    private ChatListItem? _selectedChat;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    private string _messageText =
        string.Empty;

    [ObservableProperty]
    private string _searchText =
        string.Empty;

    [ObservableProperty]
    private string _messageSearchText =
        string.Empty;

    [ObservableProperty]
    private bool _isMessageSearchVisible;

    [ObservableProperty]
    private string _messageSearchResultText =
        string.Empty;

    [ObservableProperty]
    private bool _isChatMenuOpen;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SendMessageCommand))]
    [NotifyCanExecuteChangedFor(nameof(AttachFileCommand))]
    [NotifyCanExecuteChangedFor(nameof(ClearChatCommand))]
    [NotifyCanExecuteChangedFor(nameof(DeleteChatCommand))]
    private bool _isChatOperationBusy;

    [ObservableProperty]
    private string _operationStatusText =
        string.Empty;

    [ObservableProperty]
    private bool _hasOperationStatus;

    [ObservableProperty]
    private bool _hasChats;

    [ObservableProperty]
    private bool _isChatListEmpty =
        true;

    [ObservableProperty]
    private bool _hasSelectedChat;

    [ObservableProperty]
    private bool _hasNoSelectedChat =
        true;

    public ChatsViewModel(
        string username,
        ClientProfileSnapshot localProfile,
        IChatService chatService,
        IUiDispatcher uiDispatcher,
        IFilePickerService filePickerService,
        IClientChatStateStore stateStore,
        ClientEndpointOptions endpointOptions)
    {
        Username =
            username;

        UserInitials =
            GetUserInitials(
                username);

        UserAvatarImage =
            AvatarCodec.TryCreateBitmap(
                localProfile.AvatarDataUri);

        _chatService =
            chatService ??
            throw new ArgumentNullException(
                nameof(chatService));

        _uiDispatcher =
            uiDispatcher ??
            throw new ArgumentNullException(
                nameof(uiDispatcher));

        _filePickerService =
            filePickerService ??
            throw new ArgumentNullException(
                nameof(filePickerService));

        _stateStore =
            stateStore ??
            throw new ArgumentNullException(
                nameof(stateStore));

        _syncInterval =
            TimeSpan.FromMilliseconds(
                endpointOptions.ChatSyncIntervalMilliseconds);

        Chats =
            new ObservableCollection<ChatListItem>();

        Messages =
            new ObservableCollection<MessageItem>();

        _chatService.MessageReceived +=
            OnMessageReceived;

        _ =
            InitializeAsync(
                _lifetimeCancellation.Token);
    }

    public event EventHandler?
        CreateChatRequested;

    public event EventHandler?
        SettingsRequested;

    public string Username { get; }

    public string UserInitials { get; }

    public Bitmap? UserAvatarImage { get; }

    public bool HasUserAvatar =>
        UserAvatarImage != null;

    public bool HasNoUserAvatar =>
        !HasUserAvatar;

    public ObservableCollection<ChatListItem> Chats { get; }

    public ObservableCollection<MessageItem> Messages { get; }

    partial void OnSelectedChatChanged(
        ChatListItem? value)
    {
        HasSelectedChat =
            value != null;

        HasNoSelectedChat =
            value == null;

        IsChatMenuOpen =
            false;

        HasOperationStatus =
            false;

        if (value == null)
        {
            _loadedChatId =
                null;

            _allMessages.Clear();
            Messages.Clear();

            return;
        }

        value.UnreadCount =
            0;

        if (_loadedChatId ==
            value.ChatId)
        {
            return;
        }

        _loadedChatId =
            value.ChatId;

        _ =
            LoadSelectedChatAsync(
                value.ChatId,
                _lifetimeCancellation.Token);
    }

    partial void OnSearchTextChanged(
        string value)
    {
        RebuildVisibleChats();
    }

    partial void OnMessageSearchTextChanged(
        string value)
    {
        RebuildVisibleMessages();
    }

    private async Task InitializeAsync(
        CancellationToken cancellationToken)
    {
        await RefreshChatsAsync(
            hydrateNewChats: true,
            cancellationToken);

        await SyncLoopAsync(
            cancellationToken);
    }

    private async Task SyncLoopAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(
                    _syncInterval,
                    cancellationToken);

                await RefreshChatsAsync(
                    hydrateNewChats: true,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    [RelayCommand]
    private async Task LoadChatsAsync()
    {
        await RefreshChatsAsync(
            hydrateNewChats: true,
            _lifetimeCancellation.Token);
    }

    private async Task RefreshChatsAsync(
        bool hydrateNewChats,
        CancellationToken cancellationToken)
    {
        bool entered;

        try
        {
            entered =
                await _refreshLock.WaitAsync(
                    0,
                    cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (!entered)
        {
            return;
        }

        try
        {
            IsLoading =
                true;

            IReadOnlyList<ChatListItem> serverChats =
                await _chatService.GetChatsAsync(
                    cancellationToken);

            IReadOnlySet<long> hidden =
                await _stateStore.GetHiddenChatIdsAsync(
                    cancellationToken);

            List<ChatListItem> added =
                new();

            await InvokeOnUiThreadAsync(
                () =>
                    MergeServerChats(
                        serverChats,
                        hidden,
                        added));

            if (!hydrateNewChats)
            {
                return;
            }

            foreach (ChatListItem chat in added)
            {
                await HydrateChatPreviewAsync(
                    chat,
                    cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            await InvokeOnUiThreadAsync(
                () =>
                {
                    OperationStatusText =
                        "Нет связи с сервером.";

                    HasOperationStatus =
                        true;
                });
        }
        finally
        {
            await InvokeOnUiThreadAsync(
                () =>
                    IsLoading =
                        false);

            _refreshLock.Release();
        }
    }

    private void MergeServerChats(
        IReadOnlyList<ChatListItem> serverChats,
        IReadOnlySet<long> hidden,
        ICollection<ChatListItem> added)
    {
        HashSet<long> serverIds =
            serverChats
                .Select(
                    chat =>
                        chat.ChatId)
                .ToHashSet();

        foreach (ChatListItem serverChat in serverChats)
        {
            if (hidden.Contains(
                    serverChat.ChatId))
            {
                continue;
            }

            ChatListItem? existing =
                _allChats.FirstOrDefault(
                    chat =>
                        chat.ChatId ==
                        serverChat.ChatId);

            if (existing == null)
            {
                _allChats.Add(
                    serverChat);

                added.Add(
                    serverChat);

                continue;
            }

            existing.ApplyServerMetadata(
                serverChat);
        }

        ChatListItem[] removed =
            _allChats
                .Where(
                    chat =>
                        !serverIds.Contains(chat.ChatId) ||
                        hidden.Contains(chat.ChatId))
                .ToArray();

        foreach (ChatListItem chat in removed)
        {
            _allChats.Remove(
                chat);

            Chats.Remove(
                chat);

            if (SelectedChat?.ChatId ==
                chat.ChatId)
            {
                SelectedChat =
                    null;
            }
        }

        SortChats();
        RebuildVisibleChats();
    }

    private async Task HydrateChatPreviewAsync(
        ChatListItem chat,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<MessageItem> history =
                await _chatService.GetMessagesAsync(
                    chat.ChatId,
                    cancellationToken);

            DateTime? clearedBefore =
                await _stateStore.GetClearedBeforeUtcAsync(
                    chat.ChatId,
                    cancellationToken);

            DateTime? lastRead =
                await _stateStore.GetLastReadUtcAsync(
                    chat.ChatId,
                    cancellationToken);

            MessageItem[] visible =
                ApplyClearCutoff(
                    history,
                    clearedBefore);

            MessageItem? last =
                visible.LastOrDefault();

            int unread =
                visible.Count(
                    message =>
                        !message.IsOwn &&
                        (!lastRead.HasValue ||
                         message.SentAtUtc >
                         lastRead.Value));

            await InvokeOnUiThreadAsync(
                () =>
                {
                    chat.ApplyPreview(
                        last);

                    chat.UnreadCount =
                        SelectedChat?.ChatId ==
                        chat.ChatId
                            ? 0
                            : unread;

                    SortChats();
                    RebuildVisibleChats();
                });
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task LoadSelectedChatAsync(
        long chatId,
        CancellationToken cancellationToken)
    {
        try
        {
            IReadOnlyList<MessageItem> history =
                await _chatService.GetMessagesAsync(
                    chatId,
                    cancellationToken);

            DateTime? clearedBefore =
                await _stateStore.GetClearedBeforeUtcAsync(
                    chatId,
                    cancellationToken);

            MessageItem[] visible =
                ApplyClearCutoff(
                    history,
                    clearedBefore);

            bool wasApplied =
                false;

            await InvokeOnUiThreadAsync(
                () =>
                {
                    if (_loadedChatId !=
                        chatId ||
                        SelectedChat?.ChatId !=
                        chatId)
                    {
                        return;
                    }

                    _allMessages.Clear();
                    _allMessages.AddRange(
                        visible);

                    RebuildVisibleMessages();

                    SelectedChat.UnreadCount =
                        0;

                    SelectedChat.ApplyPreview(
                        visible.LastOrDefault());

                    wasApplied =
                        true;
                });

            if (!wasApplied)
            {
                return;
            }

            DateTime readAt =
                visible.LastOrDefault()?.SentAtUtc ??
                DateTime.UtcNow;

            await _stateStore.MarkReadAsync(
                chatId,
                readAt,
                cancellationToken);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            await InvokeOnUiThreadAsync(
                () =>
                {
                    OperationStatusText =
                        "Не удалось загрузить историю.";

                    HasOperationStatus =
                        true;
                });
        }
    }

    private void OnMessageReceived(
        object? sender,
        MessageItem message)
    {
        // Important: the network receive callback never waits for another
        // network request. It only queues a UI update, preventing the old
        // event -> request -> event recursion that froze the window.
        _uiDispatcher.Post(
            () =>
            {
                ChatListItem? chat =
                    _allChats.FirstOrDefault(
                        item =>
                            item.ChatId ==
                            message.ChatId);

                if (chat == null)
                {
                    _ =
                        RestoreChatAfterIncomingMessageAsync(
                            message.ChatId);

                    return;
                }

                chat.ApplyPreview(
                    message);

                bool isActive =
                    SelectedChat?.ChatId ==
                    message.ChatId;

                if (isActive)
                {
                    bool duplicate =
                        message.MessageId > 0 &&
                        _allMessages.Any(
                            existing =>
                                existing.MessageId ==
                                message.MessageId);

                    if (!duplicate)
                    {
                        _allMessages.Add(
                            message);

                        RebuildVisibleMessages();
                    }

                    chat.UnreadCount =
                        0;

                    _ =
                        _stateStore.MarkReadAsync(
                            message.ChatId,
                            message.SentAtUtc,
                            _lifetimeCancellation.Token);
                }
                else if (!message.IsOwn)
                {
                    chat.UnreadCount++;
                }

                SortChats();
                RebuildVisibleChats();
            });
    }

    private async Task RestoreChatAfterIncomingMessageAsync(
        long chatId)
    {
        try
        {
            await _stateStore.UnhideChatAsync(
                chatId,
                _lifetimeCancellation.Token);

            await RefreshChatsAsync(
                hydrateNewChats: true,
                _lifetimeCancellation.Token);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private static MessageItem[] ApplyClearCutoff(
        IReadOnlyList<MessageItem> messages,
        DateTime? clearedBeforeUtc)
    {
        if (!clearedBeforeUtc.HasValue)
        {
            return messages.ToArray();
        }

        return messages
            .Where(
                message =>
                    message.SentAtUtc >
                    clearedBeforeUtc.Value)
            .ToArray();
    }

    private void RebuildVisibleChats()
    {
        ChatListItem[] filtered =
            string.IsNullOrWhiteSpace(SearchText)
                ? _allChats.ToArray()
                : _allChats
                    .Where(
                        chat =>
                            chat.Title.Contains(
                                SearchText,
                                StringComparison.OrdinalIgnoreCase) ||
                            chat.LastMessage.Contains(
                                SearchText,
                                StringComparison.OrdinalIgnoreCase))
                    .ToArray();

        SynchronizeVisibleCollection(
            Chats,
            filtered);

        HasChats =
            Chats.Count > 0;

        IsChatListEmpty =
            _allChats.Count == 0;

        if (SelectedChat != null &&
            !filtered.Contains(
                SelectedChat))
        {
            // Selection is cleared only when the selected chat is genuinely
            // filtered out or removed, never during an ordinary sync/reorder.
            SelectedChat =
                null;
        }
    }

    private void RebuildVisibleMessages()
    {
        string query =
            MessageSearchText.Trim();

        MessageItem[] filtered =
            string.IsNullOrWhiteSpace(query)
                ? _allMessages.ToArray()
                : _allMessages
                    .Where(
                        message =>
                            message.SearchContent.Contains(
                                query,
                                StringComparison.OrdinalIgnoreCase))
                    .ToArray();

        // Do not Clear()+re-add on every realtime message. Replacing the
        // entire ItemsControl collection resets ScrollViewer.Offset to the top.
        SynchronizeVisibleCollection(
            Messages,
            filtered);

        MessageSearchResultText =
            string.IsNullOrWhiteSpace(query)
                ? string.Empty
                : $"{filtered.Length} из {_allMessages.Count}";
    }

    private static void SynchronizeVisibleCollection<T>(
        ObservableCollection<T> target,
        IReadOnlyList<T> desired)
        where T : class
    {
        HashSet<T> desiredItems =
            new(
                desired,
                ReferenceEqualityComparer.Instance);

        for (int index = target.Count - 1;
             index >= 0;
             index--)
        {
            if (!desiredItems.Contains(
                    target[index]))
            {
                target.RemoveAt(
                    index);
            }
        }

        for (int desiredIndex = 0;
             desiredIndex < desired.Count;
             desiredIndex++)
        {
            T desiredItem =
                desired[desiredIndex];

            int currentIndex =
                target.IndexOf(
                    desiredItem);

            if (currentIndex < 0)
            {
                target.Insert(
                    desiredIndex,
                    desiredItem);

                continue;
            }

            if (currentIndex != desiredIndex)
            {
                target.Move(
                    currentIndex,
                    desiredIndex);
            }
        }
    }

    private void SortChats()
    {
        _allChats.Sort(
            (left, right) =>
                Nullable.Compare(
                    right.LastActivityUtc,
                    left.LastActivityUtc));
    }

    [RelayCommand]
    private void ToggleMessageSearch()
    {
        IsMessageSearchVisible =
            !IsMessageSearchVisible;

        if (!IsMessageSearchVisible)
        {
            MessageSearchText =
                string.Empty;
        }
    }

    [RelayCommand]
    private void CloseMessageSearch()
    {
        IsMessageSearchVisible =
            false;

        MessageSearchText =
            string.Empty;
    }

    [RelayCommand]
    private void ToggleChatMenu()
    {
        IsChatMenuOpen =
            !IsChatMenuOpen;
    }

    private bool CanSendMessage()
    {
        return SelectedChat != null &&
               !IsChatOperationBusy &&
               !string.IsNullOrWhiteSpace(
                   MessageText);
    }

    [RelayCommand(CanExecute = nameof(CanSendMessage))]
    private async Task SendMessageAsync()
    {
        if (SelectedChat == null)
        {
            return;
        }

        string text =
            MessageText.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        IsChatOperationBusy =
            true;

        try
        {
            await _chatService.SendMessageAsync(
                SelectedChat.ChatId,
                text,
                _lifetimeCancellation.Token);

            MessageText =
                string.Empty;
        }
        catch
        {
            OperationStatusText =
                "Сообщение не отправлено.";

            HasOperationStatus =
                true;
        }
        finally
        {
            IsChatOperationBusy =
                false;
        }
    }

    private bool CanAttachFile()
    {
        return SelectedChat != null &&
               !IsChatOperationBusy;
    }

    [RelayCommand(CanExecute = nameof(CanAttachFile))]
    private async Task AttachFileAsync()
    {
        if (SelectedChat == null)
        {
            return;
        }

        PickedFile? file =
            await _filePickerService.PickFileAsync(
                _lifetimeCancellation.Token);

        if (file == null)
        {
            return;
        }

        if (file.Data.Length >
            AttachmentEnvelopeCodec.MaximumFileBytes)
        {
            OperationStatusText =
                "Текущий серверный протокол позволяет отправлять файлы до 384 КБ.";

            HasOperationStatus =
                true;

            return;
        }

        IsChatOperationBusy =
            true;

        try
        {
            OperationStatusText =
                $"Отправка {file.Name}...";

            HasOperationStatus =
                true;

            await _chatService.SendFileAsync(
                SelectedChat.ChatId,
                file.Name,
                file.Data,
                _lifetimeCancellation.Token);

            OperationStatusText =
                "Файл отправлен.";
        }
        catch
        {
            OperationStatusText =
                "Файл не отправлен.";

            HasOperationStatus =
                true;
        }
        finally
        {
            IsChatOperationBusy =
                false;
        }
    }

    public async Task SaveFileAsync(
        MessageItem message)
    {
        if (!message.IsFile ||
            message.FileData.Length == 0)
        {
            return;
        }

        bool saved =
            await _filePickerService.SaveFileAsync(
                message.FileName,
                message.FileData,
                _lifetimeCancellation.Token);

        OperationStatusText =
            saved
                ? "Файл сохранён."
                : "Сохранение отменено.";

        HasOperationStatus =
            true;
    }

    private bool CanClearChat()
    {
        return SelectedChat != null &&
               !IsChatOperationBusy;
    }

    [RelayCommand(CanExecute = nameof(CanClearChat))]
    private async Task ClearChatAsync()
    {
        if (SelectedChat == null)
        {
            return;
        }

        long chatId =
            SelectedChat.ChatId;

        IsChatMenuOpen =
            false;

        await _stateStore.ClearChatAsync(
            chatId,
            DateTime.UtcNow,
            _lifetimeCancellation.Token);

        _allMessages.Clear();
        Messages.Clear();

        SelectedChat.ApplyPreview(
            null);

        SelectedChat.UnreadCount =
            0;

        OperationStatusText =
            "История очищена на этом устройстве.";

        HasOperationStatus =
            true;
    }

    private bool CanDeleteChat()
    {
        return SelectedChat != null &&
               !IsChatOperationBusy;
    }

    [RelayCommand(CanExecute = nameof(CanDeleteChat))]
    private async Task DeleteChatAsync()
    {
        if (SelectedChat == null)
        {
            return;
        }

        ChatListItem chat =
            SelectedChat;

        IsChatMenuOpen =
            false;

        await _stateStore.HideChatAsync(
            chat.ChatId,
            _lifetimeCancellation.Token);

        _allChats.Remove(
            chat);

        Chats.Remove(
            chat);

        SelectedChat =
            null;

        _loadedChatId =
            null;

        _allMessages.Clear();
        Messages.Clear();

        HasChats =
            Chats.Count > 0;

        IsChatListEmpty =
            _allChats.Count == 0;
    }

    [RelayCommand]
    private void CreateChat()
    {
        CreateChatRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        SettingsRequested?.Invoke(
            this,
            EventArgs.Empty);
    }

    private Task InvokeOnUiThreadAsync(
        Action action)
    {
        if (_uiDispatcher.CheckAccess())
        {
            action();

            return Task.CompletedTask;
        }

        TaskCompletionSource<bool> completion =
            new(
                TaskCreationOptions.RunContinuationsAsynchronously);

        _uiDispatcher.Post(
            () =>
            {
                try
                {
                    action();
                    completion.SetResult(
                        true);
                }
                catch (Exception exception)
                {
                    completion.SetException(
                        exception);
                }
            });

        return completion.Task;
    }

    private static string GetUserInitials(
        string username)
    {
        string value =
            username.Trim();

        return string.IsNullOrWhiteSpace(
                value)
            ? string.Empty
            : value[..1]
                .ToUpperInvariant();
    }

    public void Dispose()
    {
        _lifetimeCancellation.Cancel();

        _chatService.MessageReceived -=
            OnMessageReceived;

        UserAvatarImage?.Dispose();
    }
}
