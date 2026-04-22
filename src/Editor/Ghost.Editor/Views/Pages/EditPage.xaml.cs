using Ghost.Editor.Views.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Pages;

public sealed partial class EditPage : Page
{
    private readonly ContentBrowser _contentBrowser;
    private readonly LogViewer _logViewer;

    public EditPage()
    {
        InitializeComponent();

        _contentBrowser = new ContentBrowser();
        _logViewer = new LogViewer();

        ContentBrowserPresenter.Content = _contentBrowser;
    }

    private void SelectorBar_SelectionChanged(SelectorBar sender, SelectorBarSelectionChangedEventArgs args)
    {
        var selectedItem = sender.SelectedItem;
        var currentSelectedIndex = sender.Items.IndexOf(selectedItem);
        switch (currentSelectedIndex)
        {
            case 0:
                ContentBrowserPresenter.Content = _contentBrowser;
                break;
            case 2:
                ContentBrowserPresenter.Content = _logViewer;
                break;
            default:
                break;
        }
    }
}
