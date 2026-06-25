using System;

namespace Ghost.AssetBaker.Attributes;

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
