using Ghost.Core;
using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

public abstract class ComponentEditor
{
    /// <summary>
    /// Represents the underlying component object used by this class to manage its functionality.
    /// </summary>
    private readonly List<IPropertyBinding> _bindings = new();

    protected ComponentNode? ComponentNode { get; private set; }

    internal void Initialize(ComponentNode componentNode)
    {
        ComponentNode = componentNode;
    }

    /// <summary>
    /// Declarative two-way binding.
    /// </summary>
    protected void Bind<T>(
        ValueControl<T> control,
        Func<ComponentNode, T> getter,
        Action<ComponentNode, T> setter)
    {
        Logger.DebugAssert(ComponentNode != null);
        var binding = new PropertyBinding<T>(control, ComponentNode, getter, setter);
        _bindings.Add(binding);
    }

    /// <summary>
    /// Called when the component editor is created.
    /// </summary>
    /// <param name="container">The container to add the editor controls to.</param>
    public abstract void Create(Panel container);

    public virtual void Destroy() { }

    internal void SyncBindings()
    {
        foreach (var binding in _bindings)
        {
            binding.Sync();
        }
    }

}
