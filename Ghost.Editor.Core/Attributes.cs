namespace Ghost.Editor.Core;

/// <summary>
/// The base class for all attributes that can be discovered via <see cref="Utilities.TypeCache"/>.
/// </summary>
public abstract class DiscoverableAttributeBase : Attribute;


[AttributeUsage(AttributeTargets.Method)]
public class AssetOpenHandlerAttribute : DiscoverableAttributeBase
{
    public string[] Extensions
    {
        get;
    }

    public AssetOpenHandlerAttribute(params string[] extensions)
    {
        Extensions = extensions.Select(e => e.StartsWith('.') ? e.ToLowerInvariant() : '.' + e.ToLowerInvariant()).ToArray();
    }
}

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
internal class AssetImporterAttribute : DiscoverableAttributeBase
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

[AttributeUsage(AttributeTargets.Class)]
public class CustomEditorAttribute : DiscoverableAttributeBase
{
    internal Type TargetType
    {
        get;
    }

    public CustomEditorAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false, Inherited = false)]
public class EditorInjectionAttribute : DiscoverableAttributeBase
{
    public enum ServiceLifetime
    {
        Singleton,
        Transient,
        Scoped
    }

    public ServiceLifetime Lifetime
    {
        get;
    }

    public Type? ImplementationType
    {
        get;
    }

    public EditorInjectionAttribute(ServiceLifetime lifetime, Type? implementationType = null)
    {
        Lifetime = lifetime;
        ImplementationType = implementationType;
    }
}

[AttributeUsage(AttributeTargets.Method)]
public sealed class ContextMenuItemAttribute : DiscoverableAttributeBase
{
    public string Tag
    {
        get;
    }

    public string Name
    {
        get;
    }

    public int Group
    {
        get;
    }

    public ContextMenuItemAttribute(string tag, string name, int group = 0)
    {
        Tag = tag;
        Name = name;
        Group = group;
    }
}