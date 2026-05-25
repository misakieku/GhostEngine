using Ghost.Editor.Core.Utilities;
using Ghost.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Core.Inspector.Drawers;

internal class EntityDrawer : PropertyDrawer<Entity>
{
    public override FrameworkElement CreateControlT(PropertyModel<Entity> model)
    {
        var textBlock = new TextBlock
        {
            Text = $"Entity({model.Value.ID}, {model.Value.Generation})",
            VerticalAlignment = VerticalAlignment.Center
        };

        textBlock.BindOneWay(model, val => textBlock.Text = $"Entity({val.ID}, {val.Generation})");

        return textBlock;
    }
}
