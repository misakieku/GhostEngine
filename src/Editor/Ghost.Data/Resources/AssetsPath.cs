namespace Ghost.Data.Resources;

public static class AssetsPath
{
    public const string ASSETS_FOLDER = "Assets";

    public readonly static string s_appIconPath = Path.Combine(AppContext.BaseDirectory, $"{ASSETS_FOLDER}/Icon-256.ico");
}