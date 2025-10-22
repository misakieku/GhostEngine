namespace Ghost.Shader.Compiler.Parser;

internal class DefinesBlock : IBlockParser<List<Token>, List<string>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.DEFINES);
    }

    public static List<Token> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var defines = new List<Token>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var defineToken = bodyStream.Expect(TokenType.Identifier);
            defines.Add(defineToken);
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return defines;
    }

    public static List<string>? SemanticAnalysis(List<Token>? syntax, List<ShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var defines = new List<string>(syntax.Count);
        foreach (var defineToken in syntax)
        {
            defines.Add(defineToken.lexeme);
        }

        return defines;
    }
}
