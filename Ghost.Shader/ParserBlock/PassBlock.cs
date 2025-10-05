namespace Ghost.Shader.ParserBlock;

internal class PassBlock : IBlockParser<ShaderPassSyntax>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.PASS);
    }

    public static ShaderPassSyntax Parse(TokenStreamSlice stream)
    {
        var pass = new ShaderPassSyntax();

        stream.Expect(TokenType.Keyword);
        pass.name = stream.Expect(TokenType.StringLiteral);
        stream.Expect(TokenType.LBrace);

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.TryPeek(out var nextToken))
        {
            if (DefinesBlock.ShouldEnter(nextToken))
            {
                pass.defines = DefinesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (IncludesBlock.ShouldEnter(nextToken))
            {
                pass.includes = IncludesBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (KeywordsBlock.ShouldEnter(nextToken))
            {
                pass.keywords = KeywordsBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (PipelineBlock.ShouldEnter(nextToken))
            {
                pass.overridePipeline = PipelineBlock.Parse(bodyStream.SliceNextBlock());
            }
            else if (nextToken.Match(TokenType.Identifier, "vs"))
            {
                bodyStream.Expect(TokenType.Identifier, "vs");
                var vsArgs = ParseUtility.ParseFunctionArguments(ref bodyStream, TokenType.StringLiteral);
                pass.vertexShader = vsArgs[0];
                pass.vertexEntry = vsArgs[1];
                bodyStream.Expect(TokenType.Semicolon);
            }
            else if (nextToken.Match(TokenType.Identifier, "ps"))
            {
                bodyStream.Expect(TokenType.Identifier, "ps");
                var psArgs = ParseUtility.ParseFunctionArguments(ref bodyStream, TokenType.StringLiteral);
                pass.pixelShader = psArgs[0];
                pass.pixelEntry = psArgs[1];
                bodyStream.Expect(TokenType.Semicolon);
            }
            else
            {
                throw new Exception($"Unexpected token '{nextToken.lexeme}' in pass body.");
            }
        }

        stream.Expect(TokenType.RBrace);

        return pass;
    }
}
