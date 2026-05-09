using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Views.Controls;

public partial class SceneGraphTemplateSelector : DataTemplateSelector
{
    public DataTemplate? SceneNodeTemplate
    {
        get; set;
    }

    public DataTemplate? EntityNodeTemplate
    {
        get; set;
    }

    protected override DataTemplate SelectTemplateCore(object item)
    {
        var result = item switch
        {
            SceneNode => SceneNodeTemplate,
            EntityNode => EntityNodeTemplate,
            _ => base.SelectTemplateCore(item)
        };

        return result!;
    }

    protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
    {
        return SelectTemplateCore(item);
    }
}
