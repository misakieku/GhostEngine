using Microsoft.UI.Reactor.Core;

namespace Ghost.AssetForge.Views.Inspector;

public interface ICustomEditor
{
    Element Draw(object target, Action<object> onUpdate);
}
