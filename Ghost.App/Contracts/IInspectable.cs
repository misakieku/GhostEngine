using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Contracts;

public interface IInspectable
{
    public IconSource? Icon
    {
        get;
    }

    public UIElement? HeaderContent();
    public UIElement? InspectorContent();
}