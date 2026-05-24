using Ghost.Editor.Core.Controls;
using System;

namespace Ghost.Editor.Core.Inspector;

internal interface IPropertyBinding
{
    void Sync();
}

internal sealed class PropertyBinding<T> : IPropertyBinding
{
    private readonly ValueControl<T> _control;
    private readonly ComponentObject _componentObject;
    private readonly Func<ComponentObject, T> _getter;
    private readonly Action<ComponentObject, T> _setter;

    public PropertyBinding(
        ValueControl<T> control,
        ComponentObject componentObject,
        Func<ComponentObject, T> getter,
        Action<ComponentObject, T> setter)
    {
        _control = control;
        _componentObject = componentObject;
        _getter = getter;
        _setter = setter;

        // Wire user edits -> ECS write
        _control.OnValueChanged += (_, args) =>
        {
            _setter(_componentObject, args.NewValue!);
        };
    }

    public void Sync()
    {
        var current = _getter(_componentObject);
        _control.SetValueWithoutNotifying(current);
    }
}
