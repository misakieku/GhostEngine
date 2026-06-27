using System;

namespace Ghost.AssetForge.Core.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class ShowWhenAttribute : Attribute
{
    public string PropertyName { get; }
    public object Value { get; }

    public ShowWhenAttribute(string propertyName, object value)
    {
        PropertyName = propertyName;
        Value = value;
    }
}
