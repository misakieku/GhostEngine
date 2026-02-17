using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Controls;

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
public sealed partial class Float3Field : ValueControl<float3>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;

    public Float3Field()
    {
        DefaultStyleKey = typeof(Float3Field);
    }

    protected override void ValueChanged(float3 oldValue, float3 newValue)
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
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
        SuppressChangedEvent = false;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float3(
            (float)(_xComponent?.Value ?? 0),
            (float)(_yComponent?.Value ?? 0),
            (float)(_zComponent?.Value ?? 0));

        RiseChangedEvent(Value, newValue);
        Value = newValue;
    }
}
