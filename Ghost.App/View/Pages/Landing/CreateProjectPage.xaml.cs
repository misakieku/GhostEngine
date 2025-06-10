using Ghost.Editor.ViewModels.Pages.Landing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Ghost.App.View.Pages.Landing;

internal sealed partial class CreateProjectPage : Page
{
    public CreateProjectViewModel ViewModel
    {
        get;
    }

    public CreateProjectPage()
    {
        ViewModel = GhostApplication.GetService<CreateProjectViewModel>();

        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        ViewModel.OnNavigatedTo(e.Parameter);
    }
}