using Ghost.Editor.Core;

namespace Ghost.Editor.Views.Controls;

internal partial class ContentBrowser
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

    [ContextMenuItem("project-browser", "Create/Folder")]
    private static void CreateFolder()
    {
        // TODO: Use AssetService

        var viewModel = LastFocused?.ViewModel;
        if (viewModel is null)
        {
            return;
        }

        var currentDir = viewModel.CurrentDirectoryPath;
        if (!Directory.Exists(currentDir))
        {
            return;
        }

        var newFolderPath = Path.Combine(currentDir, "New Folder");
        var folderIndex = 1;
        while (Directory.Exists(newFolderPath))
        {
            newFolderPath = Path.Combine(currentDir, $"New Folder ({folderIndex})");
            folderIndex++;
        }

        Directory.CreateDirectory(newFolderPath);
        // Refresh the view model to show the new folder
        viewModel.NavigateToDirectory(currentDir);
    }
}