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

    public override void Create(StackPanel container)
    {
        _translationField = new Float3Field();
        _rotationField = new Float3Field();
        _scaleField = new Float3Field();

        _translationField.OnValueChanged += (s, e) =>
        {
            ref var data = ref ComponentObject.GetData<LocalToWorld>();
            data.matrix.c3.xyz = e.NewValue;
        };

        _rotationField.OnValueChanged += (s, e) =>
        {
            ref var data = ref ComponentObject.GetData<LocalToWorld>();
            var newRotation = quaternion.EulerXYZ(e.NewValue * math.TORADIANS);

            data.matrix.GetTRS(out var oldTranslation, out var _, out var oldScale);
            data.matrix = float4x4.TRS(oldTranslation, newRotation, oldScale);
        };

        _scaleField.OnValueChanged += (s, e) =>
        {
            ref var data = ref ComponentObject.GetData<LocalToWorld>();
            var newScale = e.NewValue;

            data.matrix.GetTRS(out var oldTranslation, out var oldRotation, out var _);
            data.matrix = float4x4.TRS(oldTranslation, oldRotation, newScale);
        };

        container.Children.Add(new PropertyField() { Label = "Position", Content = _translationField });
        container.Children.Add(new PropertyField() { Label = "Rotation", Content = _rotationField });
        container.Children.Add(new PropertyField() { Label = "Scale", Content = _scaleField });
    }

    public override void Update()
    {
        var data = ComponentObject.GetData<LocalToWorld>();
        data.matrix.GetTRS(out var position, out var rotation, out var scale);

        _translationField.Value = position;
        _rotationField.Value = math.degrees(math.EulerXYZ(rotation));
        _scaleField.Value = scale;
    }

    public override void Destroy()
    {
    }
}
