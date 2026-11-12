using Ghost.Editor.Core.Commands;
using Ghost.Editor.Core.Sync;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Studio.Drawers;

public sealed class DrawerContext
{
    public CommandHistory History { get; }
    public PropertyDrawerRegistry PropertyDrawers { get; }

    public DrawerContext(CommandHistory history, PropertyDrawerRegistry propertyDrawers)
    {
        History = history;
        PropertyDrawers = propertyDrawers;
    }

    public Element DrawProperty(PropertyNode property)
    {
        var drawer = PropertyDrawers.GetDrawer(property.PropertyType);
        return drawer.Draw(property, this);
    }

    public void ModifyProperty(PropertyNode property, object? oldValue, object? newValue)
    {
        if (Equals(oldValue, newValue))
        {
            return;
        }

        var cmd = new ModifyPropertyCommand(property, oldValue, newValue);
        History.Execute(cmd);
    }
}
