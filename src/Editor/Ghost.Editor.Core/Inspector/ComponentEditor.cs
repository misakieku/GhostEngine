using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

public abstract class ComponentEditor
{
    /// <summary>
    /// Called when the component editor is created.
    /// </summary>
    /// <param name="root">The root panel to which the editor should add its UI elements.</param>
    /// <param name="componentNode">The component node being edited.</param>
    public abstract void Create(Panel root, ComponentNode componentNode);

    public virtual void Destroy() { }
}