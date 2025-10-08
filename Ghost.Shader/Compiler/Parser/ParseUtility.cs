namespace Ghost.Shader.Compiler.Parser;

internal static class ParseUtility
{
    public static List<Token> ParseFunctionArguments(ref TokenStreamSlice stream, TokenType tokenType)
    {
        var args = new List<Token>();
        stream.Expect(TokenType.LParen);

        while (!stream.Peek().type.Equals(TokenType.RParen))
        {
            var argToken = stream.Expect(tokenType);
            args.Add(argToken);

            if (stream.Peek().type == TokenType.Comma)
            {
                stream.Consume();
            }
            else
            {
                break;
            }
        }

        stream.Expect(TokenType.RParen);
        return args;
    }

    public static FunctionCallDeclaration ParseFunction(ref TokenStreamSlice stream, TokenType tokenType)
    {
        var functionToken = stream.Expect(TokenType.Identifier);
        var args = ParseFunctionArguments(ref stream, tokenType);
        stream.Expect(TokenType.Semicolon);

        return new FunctionCallDeclaration
        {
            name = functionToken,
            arguments = args
        };
    }

    public static bool TrySliceLine(ref TokenStreamSlice stream, out TokenStreamSlice lineStream)
    {
        var length = 0;
        if (!stream.TryPeek(out var nextToken))
        {
            lineStream = default;
            return false;
        }

        while (!nextToken.Match(TokenType.Semicolon) && !nextToken.Match(TokenType.RBrace))
        {
            length++;

            if (!stream.TryPeek(length, out nextToken))
            {
                break;
            }
        }

        if (length > 0)
        {
            lineStream = stream.Slice(length);
            stream.Consume(); // Consume the semicolon
            return true;
        }

        lineStream = default;
        return false;
    }
}
