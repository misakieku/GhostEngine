using Ghost.Editor.Contracts;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Controls.Internal;

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
        this.SelectionChanged += NavigationTabView_SelectionChanged;
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
