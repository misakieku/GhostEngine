using Ghost.Editor.ViewModels.Pages.Landing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class CreateProjectPage : Page
{
    public CreateProjectViewModel ViewModel
    {
        get;
    }

    public CreateProjectPage()
    {
        ViewModel = App.GetService<CreateProjectViewModel>();

        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }

    protected override void OnNavigatedFrom(NavigationEventArgs e)
    {
        base.OnNavigatedFrom(e);
        ViewModel.OnNavigatedFrom();
    }
}