using Ghost.Editor.ViewModel.Pages.Landing;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.Editor.View.Pages.Landing;

internal sealed partial class CreateProjectPage : Page
{
    public CreateProjectViewModel ViewModel
    {
        get;
    }

    public CreateProjectPage(CreateProjectViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();
    }
}
