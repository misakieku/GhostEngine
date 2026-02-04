using Ghost.Editor.Core;

namespace Ghost.Editor.View.Controls;

internal partial class ProjectBrowser
{
    [ContextMenuItem("project-browser", "Show in Explorer")]
    private static void ShowInExplorer()
    {
        var path = LastFocused?.ViewModel.CurrentDirectoryPath;
        if (!Directory.Exists(path))
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo()
        {
            FileName = path,
            UseShellExecute = true,
            Verb = "open"
        });
    }
}