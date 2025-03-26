namespace Ghost.Data.Resources;

public class DataPath
{
    public const string ENGINE_DATA_FOLDER_NAME = "GhostEngine";

    public static string ApplicationDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ENGINE_DATA_FOLDER_NAME);
    public static string ProjectTemplatesFolder = Path.Combine(ApplicationDataFolder, "ProjectTemplates");
}