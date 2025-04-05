using Ghost.Data.Resources;
using Microsoft.UI.Xaml;
using System.IO;

namespace Ghost.Editor;

internal static class ActivationHandler
{
    private static void FolderInitialization()
    {
        if (!Directory.Exists(DataPath.APPLICATION_DATA_FOLDER))
        {
            Directory.CreateDirectory(DataPath.APPLICATION_DATA_FOLDER);
        }

        if (!Directory.Exists(DataPath.PROJECT_TEMPLATES_FOLDER))
        {
            Directory.CreateDirectory(DataPath.PROJECT_TEMPLATES_FOLDER);
        }
    }

    public static void Handle(LaunchActivatedEventArgs args)
    {
        FolderInitialization();
    }
}