using Ghost.Editor.Controls;
using Ghost.Editor.Core.Inspector;
using Ghost.Engine.Components;
using Ghost.Engine.Utilities;
using Microsoft.UI.Xaml.Controls;

namespace Ghost.Editor.Components;

[CustomEditor(typeof(LocalToWorld))]
internal class LocalToWorldEditor : IComponentEditor
{
    private Vector3Field _translationField = null!;
    private Vector3Field _rotationField = null!;
    private Vector3Field _scaleField = null!;

    public void Create(ComponentObject componentObject, StackPanel container)
    {
        _translationField = new Vector3Field();
        _rotationField = new Vector3Field();
        _scaleField = new Vector3Field();

        _translationField.OnValueChanged += (s, e) =>
        {
            var data = componentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var oldTranslation, out var oldRotation, out var oldScale);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(e.NewValue, oldRotation, oldScale);
        };

        _rotationField.OnValueChanged += (s, e) =>
        {
            var data = componentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var oldTranslation, out var oldRotation, out var oldScale);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(oldTranslation, e.NewValue.ToQuaternion(), oldScale);
        };

        _scaleField.OnValueChanged += (s, e) =>
        {
            var data = componentObject.GetData<LocalToWorld>();
            MatrixUtility.GetTRS(data.ValueRO.matrix, out var oldTranslation, out var oldRotation, out var oldScale);
            data.ValueRW.matrix = MatrixUtility.CreateTRS(oldTranslation, oldRotation, e.NewValue);
        };

        container.Children.Add(new PropertyField() { Label = "Position", Content = _translationField });
        container.Children.Add(new PropertyField() { Label = "Rotation", Content = _rotationField });
        container.Children.Add(new PropertyField() { Label = "Scale", Content = _scaleField });
    }

    public void Update(ComponentObject componentObject)
    {
        var data = componentObject.GetData<LocalToWorld>();
        MatrixUtility.GetTRS(data.ValueRO.matrix, out var translation, out var rotation, out var scale);

        if (_translationField.FocusState == Microsoft.UI.Xaml.FocusState.Unfocused)
        {
            _translationField.Value = translation;
        }

        if (_rotationField.FocusState == Microsoft.UI.Xaml.FocusState.Unfocused)
        {
            _rotationField.Value = VectorUtility.CreateFromQuaternion(rotation);
        }

        if (_scaleField.FocusState == Microsoft.UI.Xaml.FocusState.Unfocused)
        {
            _scaleField.Value = scale;
        }
    }

    public void Destroy(ComponentObject componentObject)
    {
    }
}