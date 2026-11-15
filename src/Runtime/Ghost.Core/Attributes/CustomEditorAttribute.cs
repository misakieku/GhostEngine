namespace Ghost.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CustomEditorAttribute : Attribute
{
    public Type TargetType { get; }

    public CustomEditorAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
