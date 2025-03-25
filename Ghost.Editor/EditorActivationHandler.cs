using Ghost.Editor.Constants;
using Ghost.Engine.Constants;
using Microsoft.UI.Xaml;
using System.IO;

namespace Ghost.Editor;

internal static class EditorActivationHandler
{
    private static void FolderInitialization()
    {
        if (!Directory.Exists(EngineDataPath.ApplicationDataFolder))
        {
            Directory.CreateDirectory(EngineDataPath.ApplicationDataFolder);
        }

        if (!Directory.Exists(EditorDataPath.ProjectTemplatesFolder))
        {
            Directory.CreateDirectory(EditorDataPath.ProjectTemplatesFolder);
        }
    }

    public static void Handle(LaunchActivatedEventArgs args)
    {
        FolderInitialization();
    }
}