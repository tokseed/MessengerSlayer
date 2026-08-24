using System;
using Messenger.Client.ViewModels;

namespace Messenger.Client.Services.Navigation;

public interface INavigationService
{
    ViewModelBase? CurrentViewModel { get; }

    event EventHandler? CurrentViewModelChanged;

    void NavigateTo(
        ViewModelBase viewModel);
}
