using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Messenger.Client.Bootstrap;

namespace Messenger.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            AppBootstrapper bootstrapper =
                new AppBootstrapper();

            desktop.MainWindow =
                bootstrapper.CreateMainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
