namespace Ghost.Engine.Constants;

internal static class EngineDataPath
{
    public const string ENGINE_DATA_FOLDER_NAME = "GhostEngine";

    public static string ApplicationDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ENGINE_DATA_FOLDER_NAME);
}