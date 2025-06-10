using Ghost.Editor.SceneGraph;
using Ghost.Editor.ViewModels.Pages.EngineEditor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Ghost.App.View.Pages.EngineEditor;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
internal sealed partial class HierarchyPage : Page
{
    public HierarchyViewModel ViewModel
    {
        get;
    }

    public HierarchyPage()
    {
        ViewModel = GhostApplication.GetService<HierarchyViewModel>();

        InitializeComponent();
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