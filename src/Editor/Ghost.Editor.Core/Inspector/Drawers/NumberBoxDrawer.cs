using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Numerics;

namespace Ghost.Editor.Core.Inspector.Drawers;

public sealed class NumberBoxDrawer<T> : PropertyDrawer<T>
    where T : unmanaged, INumber<T>, IMinMaxValue<T>
{
    private readonly int _fractionDigits;
    private readonly double _min;
    private readonly double _max;

    public NumberBoxDrawer(int fractionDigits, double min, double max)
    {
        _fractionDigits = fractionDigits;
        _min = min;
        _max = max;
    }

    public static unsafe NumberBoxDrawer<T> CreateFloatingPoint()
    {
        var digits = sizeof(T) > 4 ? 6 : 3;
        return new NumberBoxDrawer<T>(digits, double.CreateTruncating(T.MinValue), double.CreateTruncating(T.MaxValue));
    }

    public static NumberBoxDrawer<T> CreateInteger()
    {
        return new NumberBoxDrawer<T>(0, double.CreateTruncating(T.MinValue), double.CreateTruncating(T.MaxValue));
    }

    public override FrameworkElement CreateControlT(Ghost.Editor.Core.SceneGraph.PropertyNode<T> model)
    {
        var box = new NumberBox
        {
            SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            MaxWidth = double.PositiveInfinity, // To fill PropertyField
            Maximum = _max,
            Minimum = _min,
            Value = double.CreateTruncating(model.Value)
        };

        var formatter = new Windows.Globalization.NumberFormatting.DecimalFormatter
        {
            FractionDigits = _fractionDigits
        };
        box.NumberFormatter = formatter;

        box.ValueChanged += (s, e) =>
        {
            if (double.IsNaN(e.NewValue)) return;
            model.SetValueFromUI(T.CreateTruncating(e.NewValue));
        };

        model.OnValueChanged += (newVal) =>
        {
            box.Value = double.CreateTruncating(newVal);
        };

        return box;
    }
}
