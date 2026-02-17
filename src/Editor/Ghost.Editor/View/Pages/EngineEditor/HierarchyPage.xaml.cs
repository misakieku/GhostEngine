using Ghost.Editor.Controls;
using Ghost.Editor.Core.Contracts;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.View.Pages.EngineEditor;

internal sealed partial class HierarchyPage : NavigationTabPage
{
    private readonly IInspectorService _inspectorService;

    public HierarchyViewModel ViewModel
    {
        get;
    }

    public HierarchyPage()
    {
        _inspectorService = App.GetService<IInspectorService>();
        ViewModel = App.GetService<HierarchyViewModel>();

        InitializeComponent();
    }

    public override void OnNavigatedTo(object? parameter)
    {
        ViewModel.OnNavigatedTo(parameter);
    }

    public override void OnNavigatedFrom()
    {
        ViewModel.OnNavigatedFrom();
    }

    private void TreeView_SelectionChanged(TreeView sender, TreeViewSelectionChangedEventArgs args)
    {
        if (args.AddedItems.Count > 0 && args.AddedItems[0] is IInspectable inspectable)
        {
            _inspectorService.SetSelected(inspectable, ViewModel);
        }
        else
        {
            _inspectorService.SetSelected(null, ViewModel);
        }
    }
}

internal partial class HierarchyTemplateSector : DataTemplateSelector
{
    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (item is not SceneGraphNode node)
        {
            return base.SelectTemplateCore(item);
        }

        return node.GetSceneHierarchyTemplate();
    }
}