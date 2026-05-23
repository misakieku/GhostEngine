namespace Ghost.Editor.Core;

/// <summary>
/// The base class for all attributes that can be discovered via <see cref="Utilities.TypeCache"/>.
/// </summary>
public abstract class DiscoverableAttributeBase : Attribute;

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

public class AssetOpenHandlerAttribute : DiscoverableAttributeBase
{
    internal string[] Extensions
    {
        get;
    }

    public AssetOpenHandlerAttribute(params string[] extensions)
    {
        Extensions = extensions;
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
