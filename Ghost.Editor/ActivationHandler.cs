using Ghost.Data.Resources;
using Ghost.Data.Services;
using Ghost.Editor.Core.Utilities;
using Microsoft.UI.Xaml;

namespace Ghost.Editor;

internal static class ActivationHandler
{
    private static void FolderInitialization()
    {
        if (!Directory.Exists(DataPath.s_applicationDataFolder))
        {
            Directory.CreateDirectory(DataPath.s_applicationDataFolder);
        }

        if (!Directory.Exists(DataPath.s_projectTemplateFolder))
        {
            Directory.CreateDirectory(DataPath.s_projectTemplateFolder);
        }
    }

    public static void Handle(LaunchActivatedEventArgs args)
    {
        FolderInitialization();
        ProjectService.EnsureDefaultTemplate();

        EditorApplication.Initialize(((App)(Application.Current)).Host.Services);
    }
}