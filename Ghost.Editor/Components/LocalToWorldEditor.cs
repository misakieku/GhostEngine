using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.Inspector;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Components;

[CustomEditor(typeof(LocalToWorld))]
internal class LocalToWorldEditor : ComponentEditor
{
    private Vector3Field _translationField = null!;
    private Vector3Field _rotationField = null!;
    private Vector3Field _scaleField = null!;

    public override void Create(StackPanel container)
    {
        _translationField = new Vector3Field();
        _rotationField = new Vector3Field();
        _scaleField = new Vector3Field();

        _translationField.OnValueChanged += (s, e) =>
        {
            var data = ComponentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var _, out var oldRotation, out var oldScale);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(e.NewValue, oldRotation, oldScale);
        };

        _rotationField.OnValueChanged += (s, e) =>
        {
            var data = ComponentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var oldTranslation, out var _, out var oldScale);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(oldTranslation, e.NewValue.ToQuaternion(), oldScale);
        };

        _scaleField.OnValueChanged += (s, e) =>
        {
            var data = ComponentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var oldTranslation, out var oldRotation, out var _);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(oldTranslation, oldRotation, e.NewValue);
        };

        container.Children.Add(new PropertyField() { Label = "Position", Content = _translationField });
        container.Children.Add(new PropertyField() { Label = "Rotation", Content = _rotationField });
        container.Children.Add(new PropertyField() { Label = "Scale", Content = _scaleField });
    }

    public override void Update()
    {
        var data = ComponentObject.GetData<LocalToWorld>();
        MatrixUtility.GetTRS(data.ValueRO.matrix, out var translation, out var rotation, out var scale);

        _translationField.Value = translation;
        _rotationField.Value = VectorUtility.CreateFromQuaternion(rotation);
        _scaleField.Value = scale;
    }

    public override void Destroy()
    {
    }
}