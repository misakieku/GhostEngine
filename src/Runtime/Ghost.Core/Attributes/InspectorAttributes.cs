namespace Ghost.Core;

/// <summary>
/// Marks a field or property as read-only in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class ReadOnlyInInspectorAttribute : Attribute
{
}

/// <summary>
/// Hides a field or property from the inspector entirely.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class HideInInspectorAttribute : Attribute
{
}

/// <summary>
/// Overrides the display name for a field or property in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorNameAttribute : Attribute
{
    public string Name { get; }

    public InspectorNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Groups fields or properties under a collapsible header in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public sealed class InspectorGroupAttribute : Attribute
{
    public string GroupName { get; }

    public InspectorGroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}
