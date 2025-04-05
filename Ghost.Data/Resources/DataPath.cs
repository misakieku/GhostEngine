namespace Ghost.Data.Resources;

public class DataPath
{
    public const string ENGINE_DATA_FOLDER_NAME = "GhostEngine";

    public readonly static string APPLICATION_DATA_FOLDER = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ENGINE_DATA_FOLDER_NAME);
    public readonly static string PROJECT_TEMPLATES_FOLDER = Path.Combine(APPLICATION_DATA_FOLDER, "ProjectTemplates");
}