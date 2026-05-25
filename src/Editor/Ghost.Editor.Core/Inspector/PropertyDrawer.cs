using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Base class for type-specific property UI factories.
/// </summary>
public abstract class PropertyDrawer
{
    /// <summary>
    /// Create the UI control bound to the given property model.
    /// </summary>
    public abstract FrameworkElement CreateControl(IPropertyModel model);
}

public abstract class PropertyDrawer<T> : PropertyDrawer where T : unmanaged
{
    public sealed override FrameworkElement CreateControl(IPropertyModel model)
    {
        return CreateControlT((PropertyModel<T>)model);
    }

    public abstract FrameworkElement CreateControlT(PropertyModel<T> model);
}
