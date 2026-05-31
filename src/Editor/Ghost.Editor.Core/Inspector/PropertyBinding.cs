using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.SceneGraph;

namespace Ghost.Editor.Core.Inspector;

internal interface IPropertyBinding
{
    void Sync();
}

internal sealed class PropertyBinding<T> : IPropertyBinding
{
    private readonly ValueControl<T> _control;
    private readonly ComponentNode _componentNode;
    private readonly Func<ComponentNode, T> _getter;
    private readonly Action<ComponentNode, T> _setter;

    public PropertyBinding(
        ValueControl<T> control,
        ComponentNode componentNode,
        Func<ComponentNode, T> getter,
        Action<ComponentNode, T> setter)
    {
        _control = control;
        _componentNode = componentNode;
        _getter = getter;
        _setter = setter;

        // Wire user edits -> ECS write
        _control.OnValueChanged += (_, args) =>
        {
            _setter(_componentNode, args.NewValue);
        };
    }

    public void Sync()
    {
        var current = _getter(_componentNode);
        _control.SetValueWithoutNotifying(current);
    }
}
