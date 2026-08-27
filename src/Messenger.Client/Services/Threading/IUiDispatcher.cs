using System;
namespace Messenger.Client.Services.Threading;
public interface IUiDispatcher
{
    bool CheckAccess();
    void Post(Action action);
}
