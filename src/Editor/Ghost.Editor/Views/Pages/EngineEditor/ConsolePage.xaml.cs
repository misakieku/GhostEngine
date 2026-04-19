using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Pages.EngineEditor;

internal sealed partial class ConsolePage : Page
{
    public ConsoleViewModel ViewModel
    {
        get;
    }

    public ConsolePage()
    {
        ViewModel = App.GetService<ConsoleViewModel>();

        InitializeComponent();
    }
}