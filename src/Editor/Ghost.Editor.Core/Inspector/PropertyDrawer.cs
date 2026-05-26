using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Base class for type-specific property UI factories.
/// </summary>
public abstract class PropertyDrawer
{
    /// <summary>
    /// Create the UI control bound to the given property node.
    /// </summary>
    public abstract FrameworkElement CreateControl(PropertyNode model);
}

public abstract class PropertyDrawer<T> : PropertyDrawer where T : unmanaged
{
    public sealed override FrameworkElement CreateControl(PropertyNode model)
    {
        return CreateControlT((PropertyNode<T>)model);
    }

    public abstract FrameworkElement CreateControlT(PropertyNode<T> model);
}
