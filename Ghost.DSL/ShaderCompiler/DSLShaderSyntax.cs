namespace Ghost.DSL.ShaderCompiler;

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
    public List<Token>? propertyInitializer;
}

internal struct ValueDeclaration
{
    public Token name;
    public Token value;
}

internal struct HlslDeclaration
{
    public List<Token>? tokens;
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
    public HlslDeclaration? hlsl;
    public List<Token>? defines;
    public List<Token>? includes;
    public List<List<Token>>? keywords;
    public List<FunctionCallDeclaration>? functionCalls;
}

internal class DSLShaderSyntax
{
    public Token name;
    public PropertiesSyntax? properties;
    public PipelineSyntax? pipeline;
    public List<PassSyntax>? passes;
    public List<FunctionCallDeclaration>? functionCalls;
}