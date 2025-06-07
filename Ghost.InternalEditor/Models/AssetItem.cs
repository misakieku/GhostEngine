namespace Ghost.App.Models;

internal struct AssetItem()
{
    public string AssetPath
    {
        get; set;
    } = string.Empty;

    public string AssetName
    {
        get; set;
    } = string.Empty;

    public string IconGlyph
    {
        get; set;
    } = string.Empty;
}
