using Ghost.Editor.Core.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Numerics;

namespace Ghost.Editor.Controls;

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
public sealed partial class Vector3Field : ValueControl<Vector3>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;

    public Vector3Field()
    {
        DefaultStyleKey = typeof(Vector3Field);
    }

    protected override void ValueChanged(Vector3 oldValue, Vector3 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _zComponent?.ValueChanged -= OnComponentChanged;

        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;

        SyncFromValue();

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        SuppressChangedEvent = true;
        _xComponent?.Value = Value.X;
        _yComponent?.Value = Value.Y;
        _zComponent?.Value = Value.Z;
        SuppressChangedEvent = false;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new Vector3(
            (float)(_xComponent?.Value ?? 0),
            (float)(_yComponent?.Value ?? 0),
            (float)(_zComponent?.Value ?? 0));

        RiseChangedEvent(Value, newValue);
        Value = newValue;
    }
}