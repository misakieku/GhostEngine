using Ghost.Editor.Core.Sync;
using Ghost.Studio.DesignSystem;
using Microsoft.UI.Reactor;
using Microsoft.UI.Reactor.Core;
using Microsoft.UI.Xaml;
using static Microsoft.UI.Reactor.Factories;

namespace Ghost.Studio.Drawers;

public sealed class EnumPropertyDrawer : PropertyDrawer
{
    public override Element Draw(PropertyNode property, DrawerContext context)
    {
        var enumType = property.PropertyType;
        var names = Enum.GetNames(enumType);
        var values = Enum.GetValues(enumType);
        var current = property.Value;

        var selectedIndex = 0;
        if (current != null)
        {
            for (var i = 0; i < values.Length; i++)
            {
                if (Equals(values.GetValue(i), current))
                {
                    selectedIndex = i;
                    break;
                }
            }
        }

        var editor = ComboBox(names, selectedIndex, idx =>
        {
            if (idx >= 0 && idx < values.Length)
            {
                context.ModifyProperty(property, current, values.GetValue(idx));
            }
        })
        .HAlign(HorizontalAlignment.Stretch)
        .AutomationName(property.DisplayName);

        return DrawLabeledRow(property, editor);
    }
}
