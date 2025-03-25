// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

using Ghost.Editor.View.Pages.Landing;
using Ghost.Editor.ViewModel.Windows;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;

namespace Ghost.Editor.View.Windows;

internal sealed partial class LandingWindow : Window
{
    public LandingViewModel ViewModel
    {
        get;
    }

    private int _previousSelectedIndex;

    public LandingWindow(LandingViewModel viewModel)
    {
        ViewModel = viewModel;

        InitializeComponent();

        AppWindow.Resize(new(1200, 900));
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs e)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        var pageType = currentSelectedIndex switch
        {
            1 => typeof(CreateProjectPage),
            _ => typeof(OpenProjectPage),
        };

        var slideNavigationTransitionEffect = currentSelectedIndex - _previousSelectedIndex > 0 ?
            SlideNavigationTransitionEffect.FromRight : SlideNavigationTransitionEffect.FromLeft;

        ContentFrame.Navigate(pageType, null, new SlideNavigationTransitionInfo() { Effect = slideNavigationTransitionEffect });

        _previousSelectedIndex = currentSelectedIndex;
    }
}