using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class EmptyDrawer<T> : PropertyDrawer<T> where T : unmanaged
{
    public override FrameworkElement CreateControlT(PropertyModel<T> model)
    {
        // For a nested struct, the PropertyField will draw the Label,
        // and this empty border will be the Content (taking no space).
        // The children properties will be drawn underneath.
        return new Border();
    }
}
