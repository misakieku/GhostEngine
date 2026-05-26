using Ghost.Editor.Core.Controls;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

public abstract class ComponentEditor
{
    /// <summary>
    /// Represents the underlying component object used by this class to manage its functionality.
    /// </summary>
    private readonly List<IPropertyBinding> _bindings = new();

    protected ComponentObject ComponentObject { get; private set; }

    internal void Initialize(ComponentObject componentObject)
    {
        ComponentObject = componentObject;
    }

    /// <summary>
    /// Declarative two-way binding. Replaces manual Update().
    /// </summary>
    protected void Bind<T>(
        ValueControl<T> control,
        Func<ComponentObject, T> getter,
        Action<ComponentObject, T> setter)
    {
        var binding = new PropertyBinding<T>(control, ComponentObject, getter, setter);
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
