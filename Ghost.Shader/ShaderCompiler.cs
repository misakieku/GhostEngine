using Ghost.Shader.ParserBlock;

namespace Ghost.Shader;

public struct ShaderError
{
    public string message;
    public int line;
    public int column;

    public readonly override string ToString()
    {
        return $"Error at {line}:{column} - {message}";
    }
}

internal static class ShaderCompiler
{
    public static List<ShaderSyntax> ParseShaders(TokenStream stream)
    {
        var shaders = new List<ShaderSyntax>();

        while (stream.TryPeek(out var nextToken))
        {
            if (ShaderBlock.ShouldEnter(nextToken))
            {
                var shader = ShaderBlock.Parse(stream.SliceNextBlock());
                shaders.Add(shader);
            }
            else if (nextToken.Match(TokenType.EndOfFile))
            {
                stream.Consume();
            }
            else
            {
                throw new Exception($"Unexpected token '{nextToken.lexeme}' at top level. Expected 'shader' declaration.");
            }
        }

        return shaders;
    }

    public static ShaderModel SemanticAnalysis(ShaderSyntax syntax, out List<ShaderError> errors)
    {
        var shaderModel = new ShaderModel();
        errors = new List<ShaderError>();

        shaderModel.name = syntax.name.lexeme;

        var propertiesBlock = new PropertiesBlock();
        shaderModel.properties = propertiesBlock.SemanticAnalysis(syntax.properties, errors);

        var pipelineBlock = new PipelineBlock();
        shaderModel.pipeline = pipelineBlock.SemanticAnalysis(syntax.pipeline, errors);

        return shaderModel;
    }
}
