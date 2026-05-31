using Ghost.Editor.Core;
using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.Inspector;
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

    public override void Create(Panel container)
    {
        _translationField = new Float3Field();
        _rotationField = new Float3Field();
        _scaleField = new Float3Field();

        container.Children.Add(new PropertyField() { Label = "Position", Content = _translationField });
        container.Children.Add(new PropertyField() { Label = "Rotation", Content = _rotationField });
        container.Children.Add(new PropertyField() { Label = "Scale", Content = _scaleField });

        Bind(_translationField,
            getter: node =>
            {
                return node.GetComponent<LocalToWorld>().matrix.c3.xyz;
            },
            setter: (node, val) =>
            {
                var data = node.GetComponent<LocalToWorld>();
                data.matrix.c3.xyz = val;
                node.SetComponent(data);
            });

        Bind(_rotationField,
            getter: node =>
            {
                node.GetComponent<LocalToWorld>().matrix.GetTRS(out _, out var rotation, out _);
                return math.degrees(math.EulerXYZ(rotation));
            },
            setter: (node, val) =>
            {
                var data = node.GetComponent<LocalToWorld>();
                var newRotation = quaternion.EulerXYZ(val * math.TORADIANS);
                data.matrix.GetTRS(out var oldTranslation, out _, out var oldScale);
                data.matrix = float4x4.TRS(oldTranslation, newRotation, oldScale);
                node.SetComponent(data);
            });

        Bind(_scaleField,
            getter: node =>
            {
                var matrix = node.GetComponent<LocalToWorld>().matrix;
                var scaleX = math.length(matrix.c0.xyz);
                var scaleY = math.length(matrix.c1.xyz);
                var scaleZ = math.length(matrix.c2.xyz);
                return new float3(scaleX, scaleY, scaleZ);
            },
            setter: (node, val) =>
            {
                var data = node.GetComponent<LocalToWorld>();
                data.matrix.GetTRS(out var oldTranslation, out var oldRotation, out _);
                data.matrix = float4x4.TRS(oldTranslation, oldRotation, val);
                node.SetComponent(data);
            });
    }
}
