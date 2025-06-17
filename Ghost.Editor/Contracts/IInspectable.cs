using Microsoft.UI.Xaml;

namespace Ghost.Editor.Contracts;
internal interface IInspectable
{
    public UIElement HeaderContent();
    public UIElement InspectorContent();
}