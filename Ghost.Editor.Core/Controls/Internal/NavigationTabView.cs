using Ghost.Editor.Core.Contracts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Controls;

public partial class NavigationTabPage : TabViewItem, INavigationAware
{
    public virtual void OnNavigatedTo(object? parameter)
    {
    }

    public virtual void OnNavigatedFrom()
    {
    }
}

public sealed partial class NavigationTabView : TabView
{
    public NavigationTabView()
    {
        HorizontalAlignment = HorizontalAlignment.Stretch;
        VerticalAlignment = VerticalAlignment.Stretch;
        SelectionChanged += NavigationTabView_SelectionChanged;
    }

    private void NavigationTabView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        foreach (var oldItem in e.RemovedItems)
        {
            if (oldItem is NavigationTabPage oldPage)
            {
                oldPage.OnNavigatedFrom();
            }
        }

        if (SelectedItem is NavigationTabPage newPage)
        {
            newPage.OnNavigatedTo(null);
        }
    }
}
