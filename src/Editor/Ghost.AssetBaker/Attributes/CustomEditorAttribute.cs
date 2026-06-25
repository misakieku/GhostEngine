using System;

namespace Ghost.AssetBaker.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class CustomEditorAttribute : Attribute
{
    public Type TargetType { get; }

    public CustomEditorAttribute(Type targetType)
    {
        TargetType = targetType;
    }
}
