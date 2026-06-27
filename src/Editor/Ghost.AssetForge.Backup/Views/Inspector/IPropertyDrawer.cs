using Microsoft.UI.Reactor.Core;
using System.Reflection;

namespace Ghost.AssetForge.Views.Inspector;

public interface IPropertyDrawer
{
    Element Draw(PropertyInfo property, object target, Action<object> onUpdate);
}
