using Ghost.Editor.Core.Event;
using Microsoft.UI.Xaml;
using Misaki.HighPerformance.Mathematics;

namespace Ghost.Editor.Core.Controls;

[TemplatePart(Name = "C0Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float2Field))]
public sealed partial class Float2x2Field : ValueControl<float2x2>
{
    private Float2Field? _c0Component;
    private Float2Field? _c1Component;

    public Float2x2Field()
    {
        DefaultStyleKey = typeof(Float2x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float2x2 oldValue, float2x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float2x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double2Field))]
public sealed partial class Double2x2Field : ValueControl<double2x2>
{
    private Double2Field? _c0Component;
    private Double2Field? _c1Component;

    public Double2x2Field()
    {
        DefaultStyleKey = typeof(Double2x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double2x2 oldValue, double2x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double2x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int2Field))]
public sealed partial class Int2x2Field : ValueControl<int2x2>
{
    private Int2Field? _c0Component;
    private Int2Field? _c1Component;

    public Int2x2Field()
    {
        DefaultStyleKey = typeof(Int2x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int2x2 oldValue, int2x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int2x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint2Field))]
public sealed partial class Uint2x2Field : ValueControl<uint2x2>
{
    private Uint2Field? _c0Component;
    private Uint2Field? _c1Component;

    public Uint2x2Field()
    {
        DefaultStyleKey = typeof(Uint2x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint2x2 oldValue, uint2x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint2x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float2Field))]
public sealed partial class Float2x3Field : ValueControl<float2x3>
{
    private Float2Field? _c0Component;
    private Float2Field? _c1Component;
    private Float2Field? _c2Component;

    public Float2x3Field()
    {
        DefaultStyleKey = typeof(Float2x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float2x3 oldValue, float2x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float2x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double2Field))]
public sealed partial class Double2x3Field : ValueControl<double2x3>
{
    private Double2Field? _c0Component;
    private Double2Field? _c1Component;
    private Double2Field? _c2Component;

    public Double2x3Field()
    {
        DefaultStyleKey = typeof(Double2x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double2x3 oldValue, double2x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double2x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int2Field))]
public sealed partial class Int2x3Field : ValueControl<int2x3>
{
    private Int2Field? _c0Component;
    private Int2Field? _c1Component;
    private Int2Field? _c2Component;

    public Int2x3Field()
    {
        DefaultStyleKey = typeof(Int2x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int2x3 oldValue, int2x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int2x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint2Field))]
public sealed partial class Uint2x3Field : ValueControl<uint2x3>
{
    private Uint2Field? _c0Component;
    private Uint2Field? _c1Component;
    private Uint2Field? _c2Component;

    public Uint2x3Field()
    {
        DefaultStyleKey = typeof(Uint2x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint2x3 oldValue, uint2x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint2x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float2Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Float2Field))]
public sealed partial class Float2x4Field : ValueControl<float2x4>
{
    private Float2Field? _c0Component;
    private Float2Field? _c1Component;
    private Float2Field? _c2Component;
    private Float2Field? _c3Component;

    public Float2x4Field()
    {
        DefaultStyleKey = typeof(Float2x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float2Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Float2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float2x4 oldValue, float2x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float2x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double2Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Double2Field))]
public sealed partial class Double2x4Field : ValueControl<double2x4>
{
    private Double2Field? _c0Component;
    private Double2Field? _c1Component;
    private Double2Field? _c2Component;
    private Double2Field? _c3Component;

    public Double2x4Field()
    {
        DefaultStyleKey = typeof(Double2x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double2Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Double2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double2x4 oldValue, double2x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double2x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int2Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Int2Field))]
public sealed partial class Int2x4Field : ValueControl<int2x4>
{
    private Int2Field? _c0Component;
    private Int2Field? _c1Component;
    private Int2Field? _c2Component;
    private Int2Field? _c3Component;

    public Int2x4Field()
    {
        DefaultStyleKey = typeof(Int2x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int2Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Int2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int2x4 oldValue, int2x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int2x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint2Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Uint2Field))]
public sealed partial class Uint2x4Field : ValueControl<uint2x4>
{
    private Uint2Field? _c0Component;
    private Uint2Field? _c1Component;
    private Uint2Field? _c2Component;
    private Uint2Field? _c3Component;

    public Uint2x4Field()
    {
        DefaultStyleKey = typeof(Uint2x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint2Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint2Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint2Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Uint2Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint2x4 oldValue, uint2x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint2> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint2x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float3Field))]
public sealed partial class Float3x2Field : ValueControl<float3x2>
{
    private Float3Field? _c0Component;
    private Float3Field? _c1Component;

    public Float3x2Field()
    {
        DefaultStyleKey = typeof(Float3x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float3x2 oldValue, float3x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float3x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double3Field))]
public sealed partial class Double3x2Field : ValueControl<double3x2>
{
    private Double3Field? _c0Component;
    private Double3Field? _c1Component;

    public Double3x2Field()
    {
        DefaultStyleKey = typeof(Double3x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double3x2 oldValue, double3x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double3x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int3Field))]
public sealed partial class Int3x2Field : ValueControl<int3x2>
{
    private Int3Field? _c0Component;
    private Int3Field? _c1Component;

    public Int3x2Field()
    {
        DefaultStyleKey = typeof(Int3x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int3x2 oldValue, int3x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int3x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint3Field))]
public sealed partial class Uint3x2Field : ValueControl<uint3x2>
{
    private Uint3Field? _c0Component;
    private Uint3Field? _c1Component;

    public Uint3x2Field()
    {
        DefaultStyleKey = typeof(Uint3x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint3x2 oldValue, uint3x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint3x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float3Field))]
public sealed partial class Float3x3Field : ValueControl<float3x3>
{
    private Float3Field? _c0Component;
    private Float3Field? _c1Component;
    private Float3Field? _c2Component;

    public Float3x3Field()
    {
        DefaultStyleKey = typeof(Float3x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float3x3 oldValue, float3x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float3x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double3Field))]
public sealed partial class Double3x3Field : ValueControl<double3x3>
{
    private Double3Field? _c0Component;
    private Double3Field? _c1Component;
    private Double3Field? _c2Component;

    public Double3x3Field()
    {
        DefaultStyleKey = typeof(Double3x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double3x3 oldValue, double3x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double3x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int3Field))]
public sealed partial class Int3x3Field : ValueControl<int3x3>
{
    private Int3Field? _c0Component;
    private Int3Field? _c1Component;
    private Int3Field? _c2Component;

    public Int3x3Field()
    {
        DefaultStyleKey = typeof(Int3x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int3x3 oldValue, int3x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int3x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint3Field))]
public sealed partial class Uint3x3Field : ValueControl<uint3x3>
{
    private Uint3Field? _c0Component;
    private Uint3Field? _c1Component;
    private Uint3Field? _c2Component;

    public Uint3x3Field()
    {
        DefaultStyleKey = typeof(Uint3x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint3x3 oldValue, uint3x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint3x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float3Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Float3Field))]
public sealed partial class Float3x4Field : ValueControl<float3x4>
{
    private Float3Field? _c0Component;
    private Float3Field? _c1Component;
    private Float3Field? _c2Component;
    private Float3Field? _c3Component;

    public Float3x4Field()
    {
        DefaultStyleKey = typeof(Float3x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float3Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Float3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float3x4 oldValue, float3x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float3x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double3Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Double3Field))]
public sealed partial class Double3x4Field : ValueControl<double3x4>
{
    private Double3Field? _c0Component;
    private Double3Field? _c1Component;
    private Double3Field? _c2Component;
    private Double3Field? _c3Component;

    public Double3x4Field()
    {
        DefaultStyleKey = typeof(Double3x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double3Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Double3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double3x4 oldValue, double3x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double3x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int3Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Int3Field))]
public sealed partial class Int3x4Field : ValueControl<int3x4>
{
    private Int3Field? _c0Component;
    private Int3Field? _c1Component;
    private Int3Field? _c2Component;
    private Int3Field? _c3Component;

    public Int3x4Field()
    {
        DefaultStyleKey = typeof(Int3x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int3Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Int3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int3x4 oldValue, int3x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int3x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint3Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Uint3Field))]
public sealed partial class Uint3x4Field : ValueControl<uint3x4>
{
    private Uint3Field? _c0Component;
    private Uint3Field? _c1Component;
    private Uint3Field? _c2Component;
    private Uint3Field? _c3Component;

    public Uint3x4Field()
    {
        DefaultStyleKey = typeof(Uint3x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint3Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint3Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint3Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Uint3Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint3x4 oldValue, uint3x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint3> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint3x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float4Field))]
public sealed partial class Float4x2Field : ValueControl<float4x2>
{
    private Float4Field? _c0Component;
    private Float4Field? _c1Component;

    public Float4x2Field()
    {
        DefaultStyleKey = typeof(Float4x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float4x2 oldValue, float4x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float4x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double4Field))]
public sealed partial class Double4x2Field : ValueControl<double4x2>
{
    private Double4Field? _c0Component;
    private Double4Field? _c1Component;

    public Double4x2Field()
    {
        DefaultStyleKey = typeof(Double4x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double4x2 oldValue, double4x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double4x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int4Field))]
public sealed partial class Int4x2Field : ValueControl<int4x2>
{
    private Int4Field? _c0Component;
    private Int4Field? _c1Component;

    public Int4x2Field()
    {
        DefaultStyleKey = typeof(Int4x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int4x2 oldValue, int4x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int4x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint4Field))]
public sealed partial class Uint4x2Field : ValueControl<uint4x2>
{
    private Uint4Field? _c0Component;
    private Uint4Field? _c1Component;

    public Uint4x2Field()
    {
        DefaultStyleKey = typeof(Uint4x2Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint4x2 oldValue, uint4x2 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint4x2(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float4Field))]
public sealed partial class Float4x3Field : ValueControl<float4x3>
{
    private Float4Field? _c0Component;
    private Float4Field? _c1Component;
    private Float4Field? _c2Component;

    public Float4x3Field()
    {
        DefaultStyleKey = typeof(Float4x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float4x3 oldValue, float4x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float4x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double4Field))]
public sealed partial class Double4x3Field : ValueControl<double4x3>
{
    private Double4Field? _c0Component;
    private Double4Field? _c1Component;
    private Double4Field? _c2Component;

    public Double4x3Field()
    {
        DefaultStyleKey = typeof(Double4x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double4x3 oldValue, double4x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double4x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int4Field))]
public sealed partial class Int4x3Field : ValueControl<int4x3>
{
    private Int4Field? _c0Component;
    private Int4Field? _c1Component;
    private Int4Field? _c2Component;

    public Int4x3Field()
    {
        DefaultStyleKey = typeof(Int4x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int4x3 oldValue, int4x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int4x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint4Field))]
public sealed partial class Uint4x3Field : ValueControl<uint4x3>
{
    private Uint4Field? _c0Component;
    private Uint4Field? _c1Component;
    private Uint4Field? _c2Component;

    public Uint4x3Field()
    {
        DefaultStyleKey = typeof(Uint4x3Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint4x3 oldValue, uint4x3 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint4x3(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Float4Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Float4Field))]
public sealed partial class Float4x4Field : ValueControl<float4x4>
{
    private Float4Field? _c0Component;
    private Float4Field? _c1Component;
    private Float4Field? _c2Component;
    private Float4Field? _c3Component;

    public Float4x4Field()
    {
        DefaultStyleKey = typeof(Float4x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Float4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Float4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Float4Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Float4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(float4x4 oldValue, float4x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<float4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new float4x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Double4Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Double4Field))]
public sealed partial class Double4x4Field : ValueControl<double4x4>
{
    private Double4Field? _c0Component;
    private Double4Field? _c1Component;
    private Double4Field? _c2Component;
    private Double4Field? _c3Component;

    public Double4x4Field()
    {
        DefaultStyleKey = typeof(Double4x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Double4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Double4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Double4Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Double4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(double4x4 oldValue, double4x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<double4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new double4x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Int4Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Int4Field))]
public sealed partial class Int4x4Field : ValueControl<int4x4>
{
    private Int4Field? _c0Component;
    private Int4Field? _c1Component;
    private Int4Field? _c2Component;
    private Int4Field? _c3Component;

    public Int4x4Field()
    {
        DefaultStyleKey = typeof(Int4x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Int4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Int4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Int4Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Int4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(int4x4 oldValue, int4x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<int4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new int4x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}

[TemplatePart(Name = "C0Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C1Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C2Component", Type = typeof(Uint4Field))]
[TemplatePart(Name = "C3Component", Type = typeof(Uint4Field))]
public sealed partial class Uint4x4Field : ValueControl<uint4x4>
{
    private Uint4Field? _c0Component;
    private Uint4Field? _c1Component;
    private Uint4Field? _c2Component;
    private Uint4Field? _c3Component;

    public Uint4x4Field()
    {
        DefaultStyleKey = typeof(Uint4x4Field);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        _c0Component?.OnValueChanged -= OnComponentChanged;
        _c0Component = GetTemplateChild("C0Component") as Uint4Field;
        _c1Component?.OnValueChanged -= OnComponentChanged;
        _c1Component = GetTemplateChild("C1Component") as Uint4Field;
        _c2Component?.OnValueChanged -= OnComponentChanged;
        _c2Component = GetTemplateChild("C2Component") as Uint4Field;
        _c3Component?.OnValueChanged -= OnComponentChanged;
        _c3Component = GetTemplateChild("C3Component") as Uint4Field;

        SuppressChangedEvent = true;
        SyncFromValue();
        SuppressChangedEvent = false;

        _c0Component?.OnValueChanged += OnComponentChanged;
        _c1Component?.OnValueChanged += OnComponentChanged;
        _c2Component?.OnValueChanged += OnComponentChanged;
        _c3Component?.OnValueChanged += OnComponentChanged;
    }

    protected override void ValueChanged(uint4x4 oldValue, uint4x4 newValue)
    {
        SyncFromValue();
    }

    private void SyncFromValue()
    {
        _c0Component?.Value = Value.c0;
        _c1Component?.Value = Value.c1;
        _c2Component?.Value = Value.c2;
        _c3Component?.Value = Value.c3;
    }

    private void OnComponentChanged(object? sender, ValueChangedEventArgs<uint4> args)
    {
        if (SuppressChangedEvent)
        {
            return;
        }

        var newValue = new uint4x4(
            _c0Component?.Value ?? default,
            _c1Component?.Value ?? default,
            _c2Component?.Value ?? default,
            _c3Component?.Value ?? default
        );

        Value = newValue;
    }
}
