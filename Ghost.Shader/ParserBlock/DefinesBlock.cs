namespace Ghost.Shader.ParserBlock;

internal class DefinesBlock : IBlockParser<List<string>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.DEFINES);
    }

    public static List<string> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var defines = new List<string>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var defineToken = bodyStream.Expect(TokenType.Identifier);
            defines.Add(defineToken.lexeme);
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return defines;
    }
}
