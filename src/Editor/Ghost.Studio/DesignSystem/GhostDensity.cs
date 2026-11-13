using Microsoft.UI.Xaml;

namespace Ghost.Studio.DesignSystem;

public static class GhostDensity
{
    private static bool s_applied;

    public static void ApplyCompactDensity()
    {
        if (s_applied || Application.Current == null)
        {
            return;
        }

        s_applied = true;
        var res = new ResourceDictionary
        {
            ["TreeViewItemMinHeight"] = 26.0,
            ["ListViewItemMinHeight"] = 26.0,
            ["ScrollBarSize"] = 8.0
        };

        Application.Current.Resources.MergedDictionaries.Add(res);
    }
}
