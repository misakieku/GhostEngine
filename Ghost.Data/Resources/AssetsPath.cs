namespace Ghost.Data.Resources;

public static class AssetsPath
{
    public const string ASSETS_FOLDER = "Assets";

    public readonly static string AppIconPath = Path.Combine(AppContext.BaseDirectory, $"{ASSETS_FOLDER}/Icon-256.ico");
}