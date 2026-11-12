using Ghost.Editor.PoC.Commands;
using Ghost.Editor.PoC.Nodes;
using Microsoft.UI.Reactor.Core;

namespace Ghost.Editor.PoC.Drawers;

public sealed class DrawerContext
{
    public CommandHistory CommandHistory { get; }
    public PropertyDrawerRegistry PropertyDrawers { get; }

    public DrawerContext(CommandHistory commandHistory, PropertyDrawerRegistry propertyDrawers)
    {
        CommandHistory = commandHistory;
        PropertyDrawers = propertyDrawers;
    }

    public void ModifyProperty(PropertyNode property, object? newValue)
    {
        var oldValue = property.GetValue();
        if (Equals(oldValue, newValue))
        {
            return;
        }

        var command = new ModifyPropertyCommand(property, oldValue, newValue);
        CommandHistory.Execute(command);
    }

    public Element DrawProperty(PropertyNode property)
    {
        var drawer = PropertyDrawers.GetDrawer(property.PropertyType);
        return drawer.Draw(property, this);
    }
}
