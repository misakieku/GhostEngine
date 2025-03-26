using Ghost.Data.Resources;
using Microsoft.UI.Xaml;
using System.IO;

namespace Ghost.Editor;

internal static class ActivationHandler
{
    private static void FolderInitialization()
    {
        if (!Directory.Exists(DataPath.ApplicationDataFolder))
        {
            Directory.CreateDirectory(DataPath.ApplicationDataFolder);
        }

        if (!Directory.Exists(DataPath.ProjectTemplatesFolder))
        {
            Directory.CreateDirectory(DataPath.ProjectTemplatesFolder);
        }
    }

    public static void Handle(LaunchActivatedEventArgs args)
    {
        FolderInitialization();
    }
}