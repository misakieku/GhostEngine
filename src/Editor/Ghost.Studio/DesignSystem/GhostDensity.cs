using Microsoft.UI.Xaml;

namespace Ghost.Studio.DesignSystem;

public static class GhostDensity
{
    public static void ApplyCompactDensity()
    {
        if (Application.Current == null)
        {
            return;
        }

        var res = Application.Current.Resources;

        res["TextControlThemeMinHeight"] = 22.0;
        res["ButtonMinHeight"] = 22.0;
        res["ComboBoxMinHeight"] = 22.0;
        res["ControlContentThemeFontSize"] = 11.0;

        res["TextControlThemePadding"] = new Thickness(6, 2, 6, 2);
        res["ButtonPadding"] = new Thickness(8, 2, 8, 2);
        res["ComboBoxPadding"] = new Thickness(6, 2, 6, 2);

        res["TreeViewItemMinHeight"] = 22.0;
        res["ListViewItemMinHeight"] = 22.0;
        res["ScrollBarSize"] = 8.0;
    }
}
