namespace Ghost.Shader.ParserBlock;

internal class KeywordsBlock : IBlockParser<List<FunctionCall>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.KEYWORDS);
    }

    public static List<FunctionCall> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var keywords = new List<FunctionCall>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var keywordToken = bodyStream.Expect(TokenType.Identifier);
            var args = ParseUtility.ParseFunctionArguments(ref bodyStream, TokenType.Identifier);
            keywords.Add(new FunctionCall { name = keywordToken, arguments = args });
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return keywords;
    }
}
