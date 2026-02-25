using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Contracts;

public interface IInspectable
{
    IconSource? CreateIcon();

    UIElement? CreateHeader();

    UIElement? CreateInspector();
}