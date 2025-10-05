namespace Ghost.Shader;

public class FunctionCall
{
    public Token name;
    public List<Token> arguments = new();
}

public class PropertySyntax
{
    public Token type;
    public Token name;
    public FunctionCall? propertyConstructor;
}

public class PipelineStateSyntax
{
    public Token zTest;
    public Token zWrite;
    public Token cull;
    public Token blend;
    public Token colorMask;
}

public class ShaderPassSyntax
{
    public Token name;
    public Token vertexShader;
    public Token vertexEntry;
    public Token pixelShader;
    public Token pixelEntry;
    public List<string>? defines;
    public List<string>? includes;
    public List<FunctionCall>? keywords;
    public PipelineStateSyntax? overridePipeline;
}

public class ShaderSyntax
{
    public Token name;
    public List<PropertySyntax> properties = new();
    public PipelineStateSyntax pipeline = new();
    public List<ShaderPassSyntax> passes = new();
}
