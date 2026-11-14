using Microsoft.UI.Reactor.Core.V1Protocol.Descriptor;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TerraFX.Interop.Windows;

namespace Ghost.Studio.DesignSystem;

public static class GhostDensity
{
    private const double COMPACT_FONT_SIZE = 12.0;
    private static bool s_applied;

    public static void ApplyCompactDensity()
    {
        if (s_applied || Application.Current == null)
        {
            return;
        }

        s_applied = true;

        var res = new ResourceDictionary();
        res["TreeViewItemMinHeight"] = 26.0;
        res["ListViewItemMinHeight"] = 26.0;
        res["ScrollBarSize"] = 8.0;
        res["ControlContentThemeFontSize"] = COMPACT_FONT_SIZE;
        res["ContentControlFontSize"] = COMPACT_FONT_SIZE;
        res["TextControlThemeMinHeight"] = 24.0;
        res["TextControlThemePadding"] = new Thickness(2, 2, 6, 1);
        res["TreeViewItemMultiSelectCheckBoxMinHeight"] = 24.0;
        res["TreeViewItemPresenterMargin"] = 0;
        res["TreeViewItemPresenterPadding"] = 0;
        res["TimePickerHostPadding"] = new Thickness(0, 1, 0, 2);
        res["DatePickerHostPadding"] = new Thickness(0, 1, 0, 2);
        res["DatePickerHostMonthPadding"] = new Thickness(9, 0, 0, 1);
        res["ComboBoxEditableTextPadding"] = new Thickness(10, 0, 30, 0);
        res["ComboBoxMinHeight"] = 24.0;
        res["ComboBoxPadding"] = new Thickness(12, 1, 0, 3);
        res["NavigationViewItemOnLeftMinHeight"] = 32.0;

        res.ThemeDictionaries["Default"] = CreateThemeOverrides();
        res.ThemeDictionaries["Light"] = CreateThemeOverrides();
        res.ThemeDictionaries["HighContrast"] = CreateThemeOverrides();

        AddCompactMenuStyle<MenuFlyoutItem>(res, "DefaultMenuFlyoutItemStyle");
        AddCompactMenuStyle<MenuFlyoutSubItem>(res, "DefaultMenuFlyoutSubItemStyle");

        Application.Current.Resources.MergedDictionaries.Add(res);
    }

    private static ResourceDictionary CreateThemeOverrides()
    {
        var theme = new ResourceDictionary();
        theme["ControlContentThemeFontSize"] = COMPACT_FONT_SIZE;
        return theme;
    }

    private static void AddCompactMenuStyle<TTarget>(ResourceDictionary targetDict, string baseStyleKey)
    {
        if (!Application.Current.Resources.TryGetValue(baseStyleKey, out var baseObj) || baseObj is not Style baseStyle)
        {
            return;
        }

        var style = new Style(typeof(TTarget));
        style.BasedOn = baseStyle;
        style.Setters.Add(new Setter(Control.FontSizeProperty, COMPACT_FONT_SIZE));
        style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(10, 5, 10, 5)));
        style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 26.0));
        targetDict[typeof(TTarget)] = style;
    }
}
