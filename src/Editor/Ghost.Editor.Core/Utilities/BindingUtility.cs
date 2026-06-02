using Ghost.Editor.Core.Controls;
using Ghost.Editor.Core.SceneGraph;
using Microsoft.UI.Xaml;

namespace Ghost.Editor.Core.Utilities;

public static class BindingUtility
{
    public static void BindTwoWay<T>(this INotifyValueChanged<T> control, PropertyNode<T> node)
        where T : unmanaged
    {
        control.SetValueWithoutNotify(node.Value);
        control.OnValueChanged += (s, e) =>
        {
            node.ComponentNode.EntityNode.Modify();
            node.SetValueFromUI(e.NewValue);
        };
        node.OnValueChanged += control.SetValueWithoutNotify;
    }

    public static void BindTwoWay<T, U>(this INotifyValueChanged<T> control, PropertyNode<U> node, Func<PropertyNode<U>, T> getter, Action<PropertyNode<U>, T> setter)
        where U : unmanaged
    {
        control.SetValueWithoutNotify(getter(node));
        control.OnValueChanged += (_, args) =>
        {
            node.ComponentNode.EntityNode.Modify();
            setter(node, args.NewValue);
        };

        node.OnValueChanged += (newVal) =>
        {
            control.SetValueWithoutNotify(getter(node));
        };
    }

    public static void BindOneWay<T>(this INotifyValueChanged<T> control, PropertyNode<T> node)
        where T : unmanaged
    {
        control.SetValueWithoutNotify(node.Value);
        node.OnValueChanged += control.SetValueWithoutNotify;
    }

    public static void BindOneWay<T, U>(this INotifyValueChanged<T> control, PropertyNode<U> node, Func<PropertyNode<U>, T> getter)
        where U : unmanaged
    {
        node.OnValueChanged += (newVal) =>
        {
            control.SetValueWithoutNotify(getter(node));
        };
    }

    public static void BindOneWay<T>(this FrameworkElement element, DependencyProperty dp, PropertyNode<T> node)
        where T : unmanaged
    {
        node.OnValueChanged += (newVal) =>
        {
            element.SetValue(dp, newVal);
        };
    }
}
