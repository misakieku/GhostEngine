using Ghost.Editor.Views.Controls;
using Microsoft.UI.Xaml.Controls;
using System.Reflection;

namespace Ghost.Editor.Views.Pages;

public sealed partial class EditPage : Page
{
    private ContentBrowser? _contentBrowser;
    private LogViewer? _logViewer;

    public EditPage()
    {
        InitializeComponent();

        ContentBrowserPresenter.Content = GetContentBrowser();
    }

    private static MenuFlyoutItem CreateNewMenuItem(string name, MethodInfo methodInfo)
    {
        var menuItem = new MenuFlyoutItem { Text = name };
        menuItem.Click += (s, e) =>
        {
            methodInfo.Invoke(null, null);
        };

        return menuItem;
    }

    private ContentBrowser GetContentBrowser()
    {
        return _contentBrowser ??= new ContentBrowser();
    }

    private LogViewer GetLogViewer()
    {
        return _logViewer ??= new LogViewer();
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        switch (currentSelectedIndex)
        {
            case 0:
                ContentBrowserPresenter.Content = GetContentBrowser();
                break;
            case 2:
                ContentBrowserPresenter.Content = GetLogViewer();
                break;
            default:
                break;
        }
    }
}
