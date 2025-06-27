using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

public abstract class ComponentEditor
{
    private ComponentObject _componentObject;

    /// <summary>
    /// Represents the underlying component object used by this class to manage its functionality.
    /// </summary>
    protected ComponentObject ComponentObject => _componentObject;

    internal void Initialize(ComponentObject componentObject)
    {
        _componentObject = componentObject;
    }

    /// <summary>
    /// Called when the component editor is created.
    /// </summary>
    /// <param name="container">The container to add the editor controls to.</param>
    public virtual void Create(StackPanel container)
    {
    }

    /// <summary>
    /// Called when the component editor needs to update its UI based on the current state of the component data.
    /// </summary>
    public virtual void Update()
    {
    }

    /// <summary>
    /// Called when the component editor is destroyed.
    /// </summary>
    public virtual void Destroy()
    {
    }
}