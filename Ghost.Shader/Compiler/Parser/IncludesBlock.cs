namespace Ghost.SDL.Compiler.Parser;

internal class IncludesBlock : IBlockParser<List<Token>, List<string>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.INCLUDES);
    }

    public static List<Token> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var includes = new List<Token>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var includeToken = bodyStream.Expect(TokenType.StringLiteral);
            includes.Add(includeToken);
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return includes;
    }

    public static List<string>? SemanticAnalysis(List<Token>? syntax, List<SDLError> errors)
    {
        if (syntax == null || syntax.Count == 0)
        {
            return null;
        }

        var includes = new List<string>(syntax.Count);
        foreach (var includeToken in syntax)
        {
            var path = includeToken.lexeme;
            if (File.Exists(path))
            {
                includes.Add(path);
            }
            else
            {
                errors.Add(new SDLError
                {
                    message = $"Included file '{path}' not found.",
                    line = includeToken.line,
                    column = includeToken.column
                });
                continue;
            }
        }

        return includes;
    }
}
