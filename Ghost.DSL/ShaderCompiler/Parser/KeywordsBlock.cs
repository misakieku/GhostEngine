using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler.Parser;

internal class KeywordsBlock : IBlockParser<List<List<Token>>, List<KeywordsGroup>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.KEYWORDS);
    }

    public static List<List<Token>> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var keywords = new List<List<Token>>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var keys = new List<Token>();
            while (!bodyStream.Match(TokenType.Semicolon))
            {
                var expectType = TokenType.Identifier;
                if (keys.Count == 0)
                {
                    expectType |= TokenType.Keyword;
                }

                var argument = bodyStream.Expect(expectType);
                keys.Add(argument);

                if (bodyStream.Match(TokenType.Comma))
                {
                    bodyStream.Consume();
                }
            }

            keywords.Add(keys);
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return keywords;
    }

    public static List<KeywordsGroup>? SemanticAnalysis(List<List<Token>>? syntax, List<DSLShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var keywords = new List<KeywordsGroup>(syntax.Count);
        foreach (var keys in syntax)
        {
            if (keys.Count == 0)
            {
                continue;
            }

            var group = new KeywordsGroup();
            group.space = keys[0].lexeme switch
            {
                TokenLexicon.KnownFunctions.LOCAL => KeywordSpace.Local,
                TokenLexicon.KnownFunctions.GLOBAL => KeywordSpace.Global,
                _ => KeywordSpace.Local
            };

            for (var i = 0; i < keys.Count; i++)
            {
                var token = keys[i];
                if (i == 0 && token.type == TokenType.Keyword)
                {
                    continue;
                }

                if (token.type != TokenType.Identifier)
                {
                    errors.Add(new DSLShaderError
                    {
                        message = $"Invalid keyword '{token.lexeme}' in keywords block.",
                        line = token.line,
                        column = token.column
                    });
                    continue;
                }

                group.keywords ??= new List<string>(keys.Count);
                group.keywords.Add(token.lexeme);
            }

            keywords.Add(group);
        }

        return keywords;
    }
}
