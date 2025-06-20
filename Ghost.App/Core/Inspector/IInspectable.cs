using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector;

public interface IInspectable
{
    public IconSource? Icon
    {
        get;
    }

    public UIElement? HeaderContent
    {
        get;
    }

    public UIElement? InspectorContent
    {
        get;
    }
}