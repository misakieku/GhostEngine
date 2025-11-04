namespace Ghost.Editor.Core.Inspector;

[AttributeUsage(AttributeTargets.Class)]
public class CustomEditorAttribute(Type targetType) : Attribute
{
    internal Type TargetType
    {
        get;
    } = targetType;
}