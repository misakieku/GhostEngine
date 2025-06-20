using Ghost.Editor.Event;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Numerics;

namespace Ghost.Editor.Controls;

// TODO: value update event
public sealed partial class Vector3Field : Control
{
    private bool _suppressCallback;

    public Vector3 Value
    {
        get => new((float)X, (float)Y, (float)Z);
        set
        {
            if (value == Value)
            {
                return;
            }

            _suppressCallback = true;

            X = value.X;
            Y = value.Y;
            Z = value.Z;

            _suppressCallback = false;
        }
    }

    public double X
    {
        get => (double)GetValue(XProperty);
        set => SetValue(XProperty, value);
    }

    public static readonly DependencyProperty XProperty =
        DependencyProperty.Register(nameof(X), typeof(double), typeof(Vector3Field), new PropertyMetadata(0.0, ValueChanged));

    public double Y
    {
        get => (double)GetValue(YProperty);
        set => SetValue(YProperty, value);
    }

    public static readonly DependencyProperty YProperty =
        DependencyProperty.Register(nameof(Y), typeof(double), typeof(Vector3Field), new PropertyMetadata(0.0, ValueChanged));

    public double Z
    {
        get => (double)GetValue(ZProperty);
        set => SetValue(ZProperty, value);
    }

    public static readonly DependencyProperty ZProperty =
        DependencyProperty.Register(nameof(Z), typeof(double), typeof(Vector3Field), new PropertyMetadata(0.0, ValueChanged));

    public event ValueChangedEventHandler<Vector3>? OnValueChanged;

    private static void ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is Vector3Field vector3Field)
        {
            if (vector3Field._suppressCallback)
            {
                return;
            }

            var oldValue = vector3Field.Value;
            if (e.Property == XProperty)
            {
                var f = (float)(double)e.OldValue;
                oldValue.X = f;
            }
            else if (e.Property == YProperty)
            {
                var f = (float)(double)e.OldValue;
                oldValue.Y = f;
            }
            else if (e.Property == ZProperty)
            {
                var f = (float)(double)e.OldValue;
                oldValue.Z = f;
            }

            vector3Field.OnValueChanged?.Invoke(vector3Field, new ValueChangedEventArgs<Vector3>(oldValue, vector3Field.Value));
        }
    }

    public Vector3Field()
    {
        DefaultStyleKey = typeof(Vector3Field);
    }
}