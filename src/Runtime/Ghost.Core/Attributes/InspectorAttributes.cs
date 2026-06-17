namespace Ghost.Core;

/// <summary>
/// Marks a field as read-only in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class ReadOnlyInInspectorAttribute : Attribute
{
}

/// <summary>
/// Hides a field from the inspector entirely.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class HideInInspectorAttribute : Attribute
{
}

/// <summary>
/// Overrides the display name for a field in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class InspectorNameAttribute : Attribute
{
    public string Name { get; }

    public InspectorNameAttribute(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Groups fields under a collapsible header in the inspector.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class InspectorGroupAttribute : Attribute
{
    public string GroupName { get; }

    public InspectorGroupAttribute(string groupName)
    {
        GroupName = groupName;
    }
}
