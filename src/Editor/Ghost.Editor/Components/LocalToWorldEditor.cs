using Ghost.Editor.Core;
using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.Inspector;
using Ghost.Editor.Core.SceneGraph;
using Ghost.Editor.Core.Utilities;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Microsoft.UI.Xaml.Controls;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Components;

[CustomEditor(typeof(LocalToWorld))]
internal class LocalToWorldEditor : ComponentEditor
{
    private Float3Field _translationField = null!;
    private Float3Field _rotationField = null!;
    private Float3Field _scaleField = null!;

    public override void Create(Panel root, ComponentNode componentNode)
    {
        _translationField = new Float3Field();
        _rotationField = new Float3Field();
        _scaleField = new Float3Field();

        root.Children.Add(new PropertyField() { Label = "Position", Content = _translationField });
        root.Children.Add(new PropertyField() { Label = "Rotation", Content = _rotationField });
        root.Children.Add(new PropertyField() { Label = "Scale", Content = _scaleField });

        var property = componentNode.GetProperty<float4x4>(nameof(LocalToWorld.matrix));

        _translationField.BindTwoWay(property,
            getter: node =>
            {
                return node.Value.c3.xyz;
            },
            setter: (node, val) =>
            {
                var data = node.Value;
                data.c3.xyz = val;
                node.SetValueFromUI(data);
            });

        _rotationField.BindTwoWay(property,
            getter: node =>
            {
                node.Value.GetTRS(out _, out var rotation, out _);
                return math.degrees(math.EulerXYZ(rotation));
            },
            setter: (node, val) =>
            {
                var data = node.Value;
                var newRotation = quaternion.EulerXYZ(val * math.TORADIANS);
                data.GetTRS(out var oldTranslation, out _, out var oldScale);
                data = float4x4.TRS(oldTranslation, newRotation, oldScale);
                node.SetValueFromUI(data);
            });

        _scaleField.BindTwoWay(property,
            getter: node =>
            {
                var matrix = node.Value;
                var scaleX = math.length(matrix.c0.xyz);
                var scaleY = math.length(matrix.c1.xyz);
                var scaleZ = math.length(matrix.c2.xyz);
                return new float3(scaleX, scaleY, scaleZ);
            },
            setter: (node, val) =>
            {
                var data = node.Value;
                data.GetTRS(out var oldTranslation, out var oldRotation, out _);
                data = float4x4.TRS(oldTranslation, oldRotation, val);
                node.SetValueFromUI(data);
            });
    }
}
