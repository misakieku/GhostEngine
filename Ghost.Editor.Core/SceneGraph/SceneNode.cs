using Ghost.Editor.Core.Inspector;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.SceneGraph;

public sealed partial class SceneNode : SceneGraphNode, IInspectable
{
    public IconSource? Icon => throw new NotImplementedException();

    public UIElement? HeaderContent => throw new NotImplementedException();

    public UIElement? InspectorContent => throw new NotImplementedException();
}
