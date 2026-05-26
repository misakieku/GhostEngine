namespace Ghost.Editor.Core.Inspector;

/// <summary>
/// Marks a class as a custom property drawer for a specific type.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class CustomPropertyDrawerAttribute : DiscoverableAttributeBase
{
    public Type TargetFieldType { get; }

    public CustomPropertyDrawerAttribute(Type targetFieldType)
    {
        TargetFieldType = targetFieldType;
    }
}
