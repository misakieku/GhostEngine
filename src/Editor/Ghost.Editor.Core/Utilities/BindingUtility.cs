using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.Inspector;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Utilities;

public static class BindingUtility
{
    public static void BindTwoWay<T>(this ValueControl<T> control, IPropertyModel<T> model)
        where T : unmanaged
    {
        control.OnValueChanged += (s, e) => model.SetValueFromUI(e.NewValue);
        model.OnValueChanged += control.SetValue;
    }

    public static void BindOneWay<T>(this ValueControl<T> control, IPropertyModel<T> model)
        where T : unmanaged
    {
        model.OnValueChanged += control.SetValue;
    }

    public static void BindOneWay<T>(this FrameworkElement element, DependencyProperty dp, IPropertyModel<T> model)
        where T : unmanaged
    {
        model.OnValueChanged += (newVal) =>
        {
            element.SetValue(dp, newVal);
        };
    }

    public static void BindOneWay<T>(this FrameworkElement element, IPropertyModel<T> model, Action<T> action)
        where T : unmanaged
    {
        model.OnValueChanged += action;
    }
}
