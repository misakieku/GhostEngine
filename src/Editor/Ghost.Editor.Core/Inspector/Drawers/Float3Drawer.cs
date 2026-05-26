using Ghost.Editor.Core.Controls;
using Microsoft.UI.Xaml;

using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class Float3Drawer : PropertyDrawer<float3>
{
    public override FrameworkElement CreateControlT(Ghost.Editor.Core.SceneGraph.PropertyNode<float3> model)
    {
        var field = new Float3Field
        {
            IsEnabled = !model.Descriptor.IsReadOnly,
            Value = model.Value
        };

        field.OnValueChanged += (s, e) =>
        {
            model.SetValueFromUI(e.NewValue);
        };

        model.OnValueChanged += (newVal) =>
        {
            field.Value = newVal;
        };

        return field;
    }
}
