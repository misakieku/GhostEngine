namespace Ghost.Shader.ParserBlock;

internal class ShaderBlock : IBlockParser<ShaderSyntax>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.SHADER);
    }

    public static ShaderSyntax Parse(TokenStreamSlice stream)
    {
        var shader = new ShaderSyntax();

        stream.Expect(TokenType.Keyword);
        shader.name = stream.Expect(TokenType.StringLiteral);
        stream.Expect(TokenType.LBrace);

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.TryPeek(out var nextToken))
        {
            if (PropertiesBlock.ShouldEnter(nextToken))
            {
                shader.properties = PropertiesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PipelineBlock.ShouldEnter(nextToken))
            {
                shader.pipeline = PipelineBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PassBlock.ShouldEnter(nextToken))
            {
                shader.passes.Add(PassBlock.Parse(bodyStream.SliceNextBlock()));
            }
            else
            {
                throw new Exception($"Unexpected token '{nextToken.lexeme}' in shader body.");
            }
        }

        stream.Expect(TokenType.RBrace);

        return shader;
    }
}
