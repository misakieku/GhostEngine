namespace Ghost.Shader.ParserBlock;

internal class IncludesBlock : IBlockParser<List<string>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.INCLUDES);
    }

    public static List<string> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var includes = new List<string>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var includeToken = bodyStream.Expect(TokenType.StringLiteral);
            includes.Add(includeToken.lexeme);
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return includes;
    }
}
