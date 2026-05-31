using Ghost.Editor.Core.Controls;
using Microsoft.UI.Xaml;

using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class Float3Drawer : PropertyDrawer<float3>
{
    public override FrameworkElement CreateControlT(SceneGraph.PropertyNode<float3> node)
    {
        var field = new Float3Field
        {
            IsEnabled = !node.Descriptor.IsReadOnly,
            Value = node.Value
        };

        field.OnValueChanged += (s, e) =>
        {
            node.SetValueFromUI(e.NewValue);
        };

        node.OnValueChanged += (newVal) =>
        {
            field.Value = newVal;
        };

        return field;
    }
}
