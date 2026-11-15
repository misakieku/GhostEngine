namespace Ghost.Core;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
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
