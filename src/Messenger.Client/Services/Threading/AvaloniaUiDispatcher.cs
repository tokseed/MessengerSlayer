using System;
using Avalonia.Threading;
namespace Messenger.Client.Services.Threading;
public sealed class AvaloniaUiDispatcher : IUiDispatcher
{
    public bool CheckAccess() => Dispatcher.UIThread.CheckAccess();
    public void Post(Action action)
    {
        if (action == null) throw new ArgumentNullException(nameof(action));
        Dispatcher.UIThread.Post(action);
    }
}
