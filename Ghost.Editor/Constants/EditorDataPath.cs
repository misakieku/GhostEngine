using Ghost.Engine.Constants;
using System.IO;

namespace Ghost.Editor.Constants;

public static class EditorDataPath
{
    public static string ProjectTemplatesFolder = Path.Combine(EngineDataPath.ApplicationDataFolder, "ProjectsTemplates");
}