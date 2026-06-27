using Microsoft.UI.Xaml;

namespace Ghost.AssetForge.Views.Inspector;

public interface ICustomEditor
{
    FrameworkElement Draw(object target);
}
