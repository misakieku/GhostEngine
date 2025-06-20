using Ghost.Editor.Controls.Internal;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Services.Contracts;
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
        _inspectorService = EditorApplication.GetService<IInspectorService>();
        ViewModel = EditorApplication.GetService<HierarchyViewModel>();

        InitializeComponent();

        Header = "Hierarchy";
        IconSource = new FontIconSource
        {
            Glyph = "\uE8A4"
        };
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
            _inspectorService.SelectedInspectable = inspectable;
        }
        else
        {
            _inspectorService.SelectedInspectable = null;
        }
    }
}

internal partial class HierarchyTemplateSector : DataTemplateSelector
{
    public DataTemplate? WorldTemplate
    {
        get;
        set;
    }

    public DataTemplate? EntityTemplate
    {
        get;
        set;
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        if (WorldTemplate == null || EntityTemplate == null)
        {
            return base.SelectTemplateCore(item);
        }

        var node = (SceneGraphNode)item;
        return node.NodeType switch
        {
            SceneGraphNodeType.Scene => WorldTemplate,
            SceneGraphNodeType.Entity => EntityTemplate,
            _ => base.SelectTemplateCore(item)
        };
    }
}