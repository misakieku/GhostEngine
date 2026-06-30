namespace Ghost.Core.Graphics;

[AttributeUsage(AttributeTargets.Struct)]
public class GenerateShaderPropertyAttribute : Attribute
{
    public GenerateShaderPropertyAttribute(string shaderName, string? name = null)
    {
    }
}

[AttributeUsage(AttributeTargets.Field)]
public class GenerateAsHLSLTypeAttribute : Attribute
{
    public GenerateAsHLSLTypeAttribute(string hlslTypeName)
    {
    }
}

public enum PackingRules
{
    Exact,
    Aligned,
}

[AttributeUsage(AttributeTargets.Struct | AttributeTargets.Enum)]
public class GenerateHLSLAttribute : Attribute
{
    private readonly PackingRules _packingRules;
    private readonly string _virtualPath;

    public GenerateHLSLAttribute(PackingRules packingRules, string virtualPath)
    {
        _packingRules = packingRules;
        _virtualPath = virtualPath;
    }
}
