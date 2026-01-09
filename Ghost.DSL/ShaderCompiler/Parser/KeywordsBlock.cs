using Ghost.Core.Graphics;

namespace Ghost.DSL.ShaderCompiler.Parser;

internal class KeywordsBlock : IBlockParser<List<FunctionCallDeclaration>, List<KeywordsGroup>>
{
    public static bool ShouldEnter(Token token)
    {
        return token.Match(TokenType.Keyword, TokenLexicon.KnownKeywords.KEYWORDS);
    }

    public static List<FunctionCallDeclaration> Parse(TokenStreamSlice stream)
    {
        stream.Expect(TokenType.Keyword);
        stream.Expect(TokenType.LBrace);

        var keywords = new List<FunctionCallDeclaration>();

        var bodyStream = stream.Slice(stream.Remaining - 1);
        while (bodyStream.HasMore)
        {
            var keywordToken = bodyStream.Expect(TokenType.Identifier);
            var args = ParseUtility.ParseFunctionArguments(ref bodyStream, TokenType.Identifier);
            keywords.Add(new FunctionCallDeclaration { name = keywordToken, arguments = args });
            bodyStream.Expect(TokenType.Semicolon);
        }

        stream.Expect(TokenType.RBrace);

        return keywords;
    }

    public static List<KeywordsGroup>? SemanticAnalysis(List<FunctionCallDeclaration>? syntax, List<DSLShaderError> errors)
    {
        if (syntax == null)
        {
            return null;
        }

        var keywords = new List<KeywordsGroup>(syntax.Count);
        foreach (var keyword in syntax)
        {
            if (keyword.arguments == null || keyword.arguments.Count == 0)
            {
                errors.Add(new DSLShaderError
                {
                    message = $"Function '{keyword.name.lexeme}' must have at least one argument.",
                    line = keyword.name.line,
                    column = keyword.name.column
                });
                continue;
            }

            var group = new KeywordsGroup();
            switch (keyword.name.lexeme)
            {
                case TokenLexicon.KnownFunctions.LOCAL:
                    group.space = KeywordSpace.Local;
                    break;
                case TokenLexicon.KnownFunctions.GLOBAL:
                    group.space = KeywordSpace.Global;
                    break;
                default:
                    errors.Add(new DSLShaderError
                    {
                        message = $"Unknown function name '{keyword.name.lexeme}'.",
                        line = keyword.name.line,
                        column = keyword.name.column
                    });
                    continue;
            }

            foreach (var arg in keyword.arguments)
            {
                group.keywords ??= new List<string>(keyword.arguments.Count);
                group.keywords.Add(arg.lexeme);
            }

            keywords.Add(group);
        }

        return keywords;
    }
}
