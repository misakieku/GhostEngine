namespace Ghost.Shader.Compiler;

internal struct FunctionCallDeclaration
{
    public Token name;
    public List<Token>? arguments;
}

internal struct PropertyDeclaration
{
    public Token scope;
    public Token type;
    public Token name;
    public FunctionCallDeclaration? propertyConstructor;
}

internal struct ValueDeclaration
{
    public Token name;
    public Token value;
}

internal class PropertiesSyntax
{
    public List<PropertyDeclaration>? properties;
    public List<FunctionCallDeclaration>? functionCalls;
}

internal class PipelineSyntax
{
    public List<ValueDeclaration>? values;
    public List<FunctionCallDeclaration>? functionCalls;
}

internal class PassSyntax
{
    public Token name;
    public PipelineSyntax? localPipeline;
    public PropertiesSyntax? localProperties;
    public List<Token>? defines;
    public List<Token>? includes;
    public List<FunctionCallDeclaration>? keywords;
    public List<FunctionCallDeclaration>? functionCalls;
}

internal class ShaderSyntax
{
    public Token name;
    public PropertiesSyntax? properties;
    public PipelineSyntax? pipeline;
    public List<PassSyntax>? passes;
    public List<FunctionCallDeclaration>? functionCalls;
}