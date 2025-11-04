namespace Ghost.Editor.Core.AssetHandle;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class AssetImporterAttribute : Attribute
{
    public string[] SupportedExtensions
    {
        get;
    }

    public AssetImporterAttribute(params string[] supportedExtensions)
    {
        SupportedExtensions = supportedExtensions;
    }
}