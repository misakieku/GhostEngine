using Ghost.Editor.View.Pages.Landing;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using WinUIEx;

namespace Ghost.Editor.View.Windows;

internal sealed partial class LandingWindow : WindowEx
{
    private int _previousSelectedIndex;

    public LandingWindow()
    {
        InitializeComponent();

        this.SetWindowSize(1000, 750);
        this.CenterOnScreen();

        ExtendsContentIntoTitleBar = true;
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