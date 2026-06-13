using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Controls;


[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
public sealed partial class Float2Field : ValueControl<float2>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;

    public Float2Field()
    {
        DefaultStyleKey = typeof(Float2Field);
    }

    protected override void ValueChanged(float2 oldValue, float2 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float2(
            (float)(_xComponent?.Value ?? 0),
            (float)(_yComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

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
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
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
            (float)(_zComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "WComponent", Type = typeof(NumberBox))]
public sealed partial class Float4Field : ValueControl<float4>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;
    private NumberBox? _wComponent;

    public Float4Field()
    {
        DefaultStyleKey = typeof(Float4Field);
    }

    protected override void ValueChanged(float4 oldValue, float4 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;
        _wComponent?.ValueChanged -= OnComponentChanged;
        _wComponent = GetTemplateChild("WComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
        _wComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
        _wComponent?.Value = Value.w;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float4(
            (float)(_xComponent?.Value ?? 0),
            (float)(_yComponent?.Value ?? 0),
            (float)(_zComponent?.Value ?? 0),
            (float)(_wComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
public sealed partial class Double2Field : ValueControl<double2>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;

    public Double2Field()
    {
        DefaultStyleKey = typeof(Double2Field);
    }

    protected override void ValueChanged(double2 oldValue, double2 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double2(
            (double)(_xComponent?.Value ?? 0),
            (double)(_yComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
public sealed partial class Double3Field : ValueControl<double3>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;

    public Double3Field()
    {
        DefaultStyleKey = typeof(Double3Field);
    }

    protected override void ValueChanged(double3 oldValue, double3 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double3(
            (double)(_xComponent?.Value ?? 0),
            (double)(_yComponent?.Value ?? 0),
            (double)(_zComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "WComponent", Type = typeof(NumberBox))]
public sealed partial class Double4Field : ValueControl<double4>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;
    private NumberBox? _wComponent;

    public Double4Field()
    {
        DefaultStyleKey = typeof(Double4Field);
    }

    protected override void ValueChanged(double4 oldValue, double4 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;
        _wComponent?.ValueChanged -= OnComponentChanged;
        _wComponent = GetTemplateChild("WComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
        _wComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
        _wComponent?.Value = Value.w;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double4(
            (double)(_xComponent?.Value ?? 0),
            (double)(_yComponent?.Value ?? 0),
            (double)(_zComponent?.Value ?? 0),
            (double)(_wComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
public sealed partial class Int2Field : ValueControl<int2>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;

    public Int2Field()
    {
        DefaultStyleKey = typeof(Int2Field);
    }

    protected override void ValueChanged(int2 oldValue, int2 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int2(
            (int)(_xComponent?.Value ?? 0),
            (int)(_yComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
public sealed partial class Int3Field : ValueControl<int3>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;

    public Int3Field()
    {
        DefaultStyleKey = typeof(Int3Field);
    }

    protected override void ValueChanged(int3 oldValue, int3 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int3(
            (int)(_xComponent?.Value ?? 0),
            (int)(_yComponent?.Value ?? 0),
            (int)(_zComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "WComponent", Type = typeof(NumberBox))]
public sealed partial class Int4Field : ValueControl<int4>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;
    private NumberBox? _wComponent;

    public Int4Field()
    {
        DefaultStyleKey = typeof(Int4Field);
    }

    protected override void ValueChanged(int4 oldValue, int4 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;
        _wComponent?.ValueChanged -= OnComponentChanged;
        _wComponent = GetTemplateChild("WComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
        _wComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
        _wComponent?.Value = Value.w;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int4(
            (int)(_xComponent?.Value ?? 0),
            (int)(_yComponent?.Value ?? 0),
            (int)(_zComponent?.Value ?? 0),
            (int)(_wComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
public sealed partial class Uint2Field : ValueControl<uint2>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;

    public Uint2Field()
    {
        DefaultStyleKey = typeof(Uint2Field);
    }

    protected override void ValueChanged(uint2 oldValue, uint2 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint2(
            (uint)(_xComponent?.Value ?? 0),
            (uint)(_yComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
public sealed partial class Uint3Field : ValueControl<uint3>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;

    public Uint3Field()
    {
        DefaultStyleKey = typeof(Uint3Field);
    }

    protected override void ValueChanged(uint3 oldValue, uint3 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint3(
            (uint)(_xComponent?.Value ?? 0),
            (uint)(_yComponent?.Value ?? 0),
            (uint)(_zComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "XComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "YComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "ZComponent", Type = typeof(NumberBox))]
[TemplatePart(Name = "WComponent", Type = typeof(NumberBox))]
public sealed partial class Uint4Field : ValueControl<uint4>
{
    private NumberBox? _xComponent;
    private NumberBox? _yComponent;
    private NumberBox? _zComponent;
    private NumberBox? _wComponent;

    public Uint4Field()
    {
        DefaultStyleKey = typeof(Uint4Field);
    }

    protected override void ValueChanged(uint4 oldValue, uint4 newValue)
    {
        SyncFromValue();
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _xComponent?.ValueChanged -= OnComponentChanged;
        _xComponent = GetTemplateChild("XComponent") as NumberBox;
        _yComponent?.ValueChanged -= OnComponentChanged;
        _yComponent = GetTemplateChild("YComponent") as NumberBox;
        _zComponent?.ValueChanged -= OnComponentChanged;
        _zComponent = GetTemplateChild("ZComponent") as NumberBox;
        _wComponent?.ValueChanged -= OnComponentChanged;
        _wComponent = GetTemplateChild("WComponent") as NumberBox;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _xComponent?.ValueChanged += OnComponentChanged;
        _yComponent?.ValueChanged += OnComponentChanged;
        _zComponent?.ValueChanged += OnComponentChanged;
        _wComponent?.ValueChanged += OnComponentChanged;
    }

    private void SyncFromValue()
    {
        _xComponent?.Value = Value.x;
        _yComponent?.Value = Value.y;
        _zComponent?.Value = Value.z;
        _wComponent?.Value = Value.w;
    }

    private void OnComponentChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint4(
            (uint)(_xComponent?.Value ?? 0),
            (uint)(_yComponent?.Value ?? 0),
            (uint)(_zComponent?.Value ?? 0),
            (uint)(_wComponent?.Value ?? 0)
        );

        Value = newValue;
    }
}
