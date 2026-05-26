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
            getter: obj =>
            {
                obj.GetData<LocalToWorld>().matrix.GetTRS(out var position, out _, out _);
                return position;
            },
            setter: (obj, val) =>
            {
                ref var data = ref obj.GetData<LocalToWorld>();
                data.matrix.c3.xyz = val;
            });

        Bind(_rotationField,
            getter: obj =>
            {
                obj.GetData<LocalToWorld>().matrix.GetTRS(out _, out var rotation, out _);
                return math.degrees(math.EulerXYZ(rotation));
            },
            setter: (obj, val) =>
            {
                ref var data = ref obj.GetData<LocalToWorld>();
                var newRotation = quaternion.EulerXYZ(val * math.TORADIANS);
                data.matrix.GetTRS(out var oldTranslation, out _, out var oldScale);
                data.matrix = float4x4.TRS(oldTranslation, newRotation, oldScale);
            });

        Bind(_scaleField,
            getter: obj =>
            {
                obj.GetData<LocalToWorld>().matrix.GetTRS(out _, out _, out var scale);
                return scale;
            },
            setter: (obj, val) =>
            {
                ref var data = ref obj.GetData<LocalToWorld>();
                data.matrix.GetTRS(out var oldTranslation, out var oldRotation, out _);
                data.matrix = float4x4.TRS(oldTranslation, oldRotation, val);
            });
    }
}
