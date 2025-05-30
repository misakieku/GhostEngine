namespace Ghost.Data.Resources;

public class DataPath
{
    public const string ENGINE_DATA_FOLDER_NAME = "GhostEngine";

    public readonly static string s_applicationDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ENGINE_DATA_FOLDER_NAME);
    public readonly static string s_projectTemplateFolder = Path.Combine(s_applicationDataFolder, "ProjectTemplates");
}