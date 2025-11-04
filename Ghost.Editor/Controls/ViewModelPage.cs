using CommunityToolkit.Mvvm.ComponentModel;
using Ghost.Editor.Core.Contracts;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Ghost.Editor.Controls;

public abstract partial class ViewModelPage<VM> : Page
    where VM : ObservableObject
{
    public VM ViewModel
    {
        get;
    }

    protected ViewModelPage(VM viewModel)
    {
        ViewModel = viewModel;
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (ViewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedTo(e.Parameter);
        }
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        if (ViewModel is INavigationAware navigationAware)
        {
            navigationAware.OnNavigatedFrom();
        }
    }
}