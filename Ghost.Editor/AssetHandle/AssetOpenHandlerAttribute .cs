namespace Ghost.Editor.AssetHandle;

[AttributeUsage(AttributeTargets.Method)]
public class AssetOpenHandlerAttribute : Attribute
{
    public string[] Extensions
    {
        get;
    }

    public AssetOpenHandlerAttribute(params string[] extensions)
    {
        Extensions = extensions.Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()).ToArray();
    }
}

[AttributeUsage(AttributeTargets.Method)]
public class AsyncAssetOpenHandlerAttribute : Attribute
{
    public string[] Extensions
    {
        get;
    }

    public AsyncAssetOpenHandlerAttribute(params string[] extensions)
    {
        Extensions = extensions.Select(e => e.StartsWith(".") ? e.ToLowerInvariant() : "." + e.ToLowerInvariant()).ToArray();
    }
}