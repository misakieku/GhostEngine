using System;
using System.Reflection;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;

namespace Ghost.AssetBaker.Views.Inspector;

public interface IPropertyDrawer
{
    Element Draw(PropertyInfo property, object target, Action<object> onUpdate);
}
