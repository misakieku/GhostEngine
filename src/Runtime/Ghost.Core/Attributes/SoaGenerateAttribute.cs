namespace Ghost.Core;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class SoaGenerateAttribute : Attribute
{
    public SoaGenerateAttribute(bool unmanaged)
    {
    }
}