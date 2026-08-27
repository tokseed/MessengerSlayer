using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
namespace Messenger.Client.Views;
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        PropertyChanged += OnWindowPropertyChanged;
        UpdateMaximizeRestoreIcon();
    }
    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs eventArgs)
    {
        PointerPoint point = eventArgs.GetCurrentPoint(this);
        if (!point.Properties.IsLeftButtonPressed) return;
        BeginMoveDrag(eventArgs);
    }
    private void MinimizeButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => WindowState = WindowState.Minimized;
    private void MaximizeRestoreButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs)
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        UpdateMaximizeRestoreIcon();
    }
    private void CloseButton_OnClick(object? sender, Avalonia.Interactivity.RoutedEventArgs eventArgs) => Close();
    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs eventArgs)
    {
        if (eventArgs.Property == WindowStateProperty) UpdateMaximizeRestoreIcon();
    }
    private void UpdateMaximizeRestoreIcon()
    {
        bool maximized = WindowState == WindowState.Maximized;
        MaximizeIcon.IsVisible = !maximized;
        RestoreIcon.IsVisible = maximized;
        ToolTip.SetTip(MaximizeRestoreButton, maximized ? "Восстановить" : "Развернуть");
    }
}
