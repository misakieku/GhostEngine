using System;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;

namespace Ghost.AssetBaker.Views.Inspector;

public interface ICustomEditor
{
    Element Draw(object target, Action<object> onUpdate);
}
