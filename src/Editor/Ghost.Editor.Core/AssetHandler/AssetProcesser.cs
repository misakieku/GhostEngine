using Ghost.Editor.Core.Contracts;

namespace Ghost.Editor.Core.AssetHandler;

[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomAssetProcesserAttribute<T> : Attribute
{
    public Type Type => typeof(T);
}

public readonly struct AssetProcesserContext
{
    public IAssetRegistry Registry
    {
        get; init;
    }

    public string AssetPath
    {
        get; init;
    }

    public Asset Asset
    {
        get; init;
    }

    public IAssetHandler Handler
    {
        get; init;
    }
}

public interface IAssetProcesser
{
    ValueTask ProcessAsync(AssetProcesserContext ctx);
}