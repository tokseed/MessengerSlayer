using System;
using Messenger.Client.ViewModels;

namespace Messenger.Client.Services.Navigation;

public sealed class NavigationService :
    INavigationService
{
    private ViewModelBase? _currentViewModel;

    public ViewModelBase? CurrentViewModel =>
        _currentViewModel;

    public event EventHandler?
        CurrentViewModelChanged;

    public void NavigateTo(
        ViewModelBase viewModel)
    {
        _currentViewModel =
            viewModel ??
            throw new ArgumentNullException(
                nameof(viewModel));

        CurrentViewModelChanged?.Invoke(
            this,
            EventArgs.Empty);
    }
}
