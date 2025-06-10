using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.App.View.Pages.EngineEditor;

internal sealed partial class ConsolePage : Page
{
    public ConsoleViewModel ViewModel
    {
        get;
    }

    public ConsolePage()
    {
        ViewModel = GhostApplication.GetService<ConsoleViewModel>();

        InitializeComponent();
    }
}
