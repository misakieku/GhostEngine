using Microsoft.UI.Xaml;
using System.Reflection;

namespace Ghost.AssetForge.Views.Inspector;

public interface IPropertyDrawer
{
    FrameworkElement Draw(PropertyInfo property, object target);
}
