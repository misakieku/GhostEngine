using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Contracts;

public interface IInspectable
{
    public IconSource? CreateIcon();

    public UIElement? CreateHeader();

    public UIElement? CreateInspector();
}