using Windows.System;

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

    public int Priority
    {
        get;
    }

    public ContextMenuItemAttribute(string tag, string name, int group = 0, int priority = 0)
    {
        Tag = tag;
        Name = name;
        Group = group;
        Priority = priority;
    }
}

public sealed class ShortcutAttribute : DiscoverableAttributeBase
{
    public VirtualKey Key
    {
        get;
    }

    public VirtualKeyModifiers Modifiers
    {
        get;
    }

    public ShortcutAttribute(VirtualKey key, VirtualKeyModifiers modifiers = VirtualKeyModifiers.None)
    {
        Key = key;
        Modifiers = modifiers;
    }
}