using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Messenger.Client.UIModels;
using Messenger.Client.ViewModels;

namespace Messenger.Client.Views;

public partial class ChatsView : UserControl
{
    public ChatsView()
    {
        InitializeComponent();

        MessageComposer.AddHandler(
            InputElement.KeyDownEvent,
            MessageComposer_OnKeyDown,
            RoutingStrategies.Tunnel,
            handledEventsToo: true);
    }

    private void MessageComposer_OnKeyDown(
        object? sender,
        KeyEventArgs eventArgs)
    {
        if (eventArgs.Key != Key.Enter)
        {
            return;
        }

        if (eventArgs.KeyModifiers.HasFlag(
                KeyModifiers.Shift))
        {
            // Shift+Enter remains available for a multiline message.
            return;
        }

        if (DataContext is not ChatsViewModel viewModel)
        {
            return;
        }

        if (!viewModel.SendMessageCommand.CanExecute(
                null))
        {
            return;
        }

        eventArgs.Handled =
            true;

        viewModel.SendMessageCommand.Execute(
            null);
    }

    private async void SaveFileButton_OnClick(object? sender, RoutedEventArgs eventArgs)
    {
        if (sender is not Button { DataContext: MessageItem message })
        {
            return;
        }

        if (DataContext is not ChatsViewModel viewModel)
        {
            return;
        }

        await viewModel.SaveFileAsync(message);
    }
}
