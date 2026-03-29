using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Models;
using Ghost.Editor.ViewModels.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;

namespace Ghost.Editor.View.Controls;

internal sealed partial class ProjectBrowser : UserControl
{
    public static ProjectBrowser? LastFocused
    {
        get;
        private set;
    }

    private readonly IInspectorService _inspectorService;
    private bool _isUpdatingSelection = false;

    public ProjectBrowserViewModel ViewModel
    {
        get;
    }

    public ProjectBrowser()
    {
        _inspectorService = App.GetService<IInspectorService>();
        ViewModel = App.GetService<ProjectBrowserViewModel>();

        InitializeComponent();

        Loaded += ProjectBrowser_Loaded;
        Unloaded += ProjectBrowser_Unloaded;

        GettingFocus += ProjectBrowser_GettingFocus;
    }

    private void ProjectBrowser_GettingFocus(UIElement sender, GettingFocusEventArgs args)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        LastFocused = this;
    }

    private void ProjectBrowser_Loaded(object sender, RoutedEventArgs e)
    {
        _inspectorService.OnSelectionChanged += _inspectorService_OnSelectionChanged;

        // HACK: Scroll a little bit to trigger ScrollView to update it's scrollbar visibility. Otherwise the scrollbar will show a incorrect state when docking layout changes.
        PART_FilesView.ScrollView.ScrollBy(1, 0);
    }

    private void ProjectBrowser_Unloaded(object sender, RoutedEventArgs e)
    {
        _inspectorService.OnSelectionChanged -= _inspectorService_OnSelectionChanged;

        if (LastFocused == this)
        {
            LastFocused = null;
        }
    }

    private void _inspectorService_OnSelectionChanged(object? sender, InspectorSelectionChangedEventArgs e)
    {
        if (e.Source is not ProjectBrowserViewModel)
        {
            PART_FilesView.DeselectAll();
            PART_DirectoriesView.SelectedNodes.Clear();
        }
    }

    private void PART_DirectoriesView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        _isUpdatingSelection = true;

        PART_FilesView.DeselectAll();
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is ExplorerItem selectedItem)
        {
            ViewModel.SelectedItem = selectedItem;
            ViewModel.NavigateToDirectory(selectedItem.FullName);
        }

        _isUpdatingSelection = false;
    }

    private void PART_FilesView_SelectionChanged(ItemsView sender, ItemsViewSelectionChangedEventArgs args)
    {
        if (_isUpdatingSelection)
        {
            return;
        }

        _isUpdatingSelection = true;

        PART_DirectoriesView.SelectedNodes.Clear();
        if (PART_FilesView.SelectedItem is ExplorerItem selectedItem)
        {
            ViewModel.SelectedItem = selectedItem;
        }

        _isUpdatingSelection = false;
    }

    private async void PART_FilesView_DoubleTapped(object sender, Microsoft.UI.Xaml.Input.DoubleTappedRoutedEventArgs e)
    {
        if (PART_FilesView.SelectedItem is ExplorerItem selectedItem)
        {
            _isUpdatingSelection = true;

            PART_FilesView.DeselectAll();
            PART_DirectoriesView.SelectedNodes.Clear();

            // NOTE: There is bug that the hover state of the item may remain when navigating to another folder.
            // Which causes incorrect selection (double click a folder -> directories view select target folder -> files view select the item that has the same index as the double clicked one and the hover visual stay remain from last level)
            // Not sure if this is a WinUI bug or something else. This may because of the virtualization of the ItemsView.
            // The core issue is not sure why PART_FilesView_SelectionChanged been triggered after NavigateToDirectory is already finished. And this only happens after the first double click navigation (first time is always fine).

            // HACK: Wait a moment to let the ui clear it's state, otherwise the bug above will happen because of the virtualization.
            await Task.Delay(100);

            ViewModel.SelectedItem = selectedItem;

            var navigatedItem = ViewModel.OpenSelected();
            if (navigatedItem.Item1 != null)
            {
                if (navigatedItem.Item2 == 0)
                {
                    PART_DirectoriesView.SelectedItem = navigatedItem.Item1;
                }
                else if (navigatedItem.Item2 == 1)
                {
                    var index = ViewModel.Files.IndexOf(navigatedItem.Item1);
                    PART_FilesView.Select(index);
                }
            }

            _isUpdatingSelection = false;
        }
    }
}