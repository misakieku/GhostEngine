namespace Ghost.DSL.ShaderCompiler;

internal static class TokenStreamImple
{
    public static Token Peek(ReadOnlySpan<Token> tokens, int index, int length)
    {
        if (index + length < tokens.Length)
        {
            return tokens[index + length];
        }

        throw new IndexOutOfRangeException();
    }

    public static bool TryPeek(ReadOnlySpan<Token> tokens, int index, int length, out Token token)
    {
        if (index + length < tokens.Length)
        {
            token = tokens[index + length];
            return true;
        }

        token = default;
        return false;
    }

    public static bool TryConsume(ReadOnlySpan<Token> tokens, ref int index, out Token token)
    {
        if (index < tokens.Length)
        {
            token = tokens[index++];
            return true;
        }

        token = default;
        return false;
    }

    public static Token Consume(ReadOnlySpan<Token> tokens, ref int index, int count = 1)
    {
        if (index + count <= tokens.Length)
        {
            index += count;
            return tokens[index - 1];
        }

        throw new IndexOutOfRangeException();
    }

    public static bool Match(ReadOnlySpan<Token> tokens, int index, TokenType type, string? lexeme)
    {
        var t = Peek(tokens, index, 0);
        if (!t.Match(type, lexeme))
        {
            return false;
        }

        return true;
    }

    public static int MatchMany(ReadOnlySpan<Token> tokens, int index, TokenType type, string? lexeme)
    {
        var count = 0;
        while (TryPeek(tokens, index, 0, out var t) && t.Match(type, lexeme))
        {
            count++;
        }

        return count;
    }

    public static Token Expect(ReadOnlySpan<Token> tokens, ref int index, TokenType type, string? lexeme)
    {
        if (!TryPeek(tokens, index, 0, out var t))
        {
            throw new InvalidOperationException("Expected token but reached end of stream");
        }

        if (!t.Match(type, lexeme))
        {
            throw new InvalidOperationException($"Expected token {type}('{lexeme ?? "*"}') but got {t}");
        }

        index++;
        return t;
    }
}

internal class TokenStream
{
    private readonly Token[] _tokens;
    private int _index = 0;

    public int Length => _tokens.Length;
    public int Remaining => _tokens.Length - _index;
    public bool HasMore => _index < _tokens.Length;
    public int Position
    {
        get => _index;
        set
        {
            if (value < 0 || value > _tokens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the bounds of the token stream.");
            }
            _index = value;
        }
    }

    public TokenStream(Token[] tokens)
    {
        _tokens = tokens;
    }

    public TokenStream(IEnumerable<Token> tokens)
    {
        _tokens = tokens.ToArray();
    }

    public Token Peek(int length = 0)
    {
        return TokenStreamImple.Peek(_tokens, _index, length);
    }

    public bool TryPeek(out Token token)
    {
        return TokenStreamImple.TryPeek(_tokens, _index, 0, out token);
    }

    public bool TryPeek(int length, out Token token)
    {
        return TokenStreamImple.TryPeek(_tokens, _index, length, out token);
    }

    public bool TryConsume(out Token token)
    {
        return TokenStreamImple.TryConsume(_tokens, ref _index, out token);
    }

    public Token Consume(int count = 1)
    {
        return TokenStreamImple.Consume(_tokens, ref _index, count);
    }

    public bool Match(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.Match(_tokens, _index, type, lexeme);
    }

    public int MatchMany(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.MatchMany(_tokens, _index, type, lexeme);
    }

    public Token Expect(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.Expect(_tokens, ref _index, type, lexeme);
    }

    public TokenStreamSlice Slice(int length = -1)
    {
        if (length <= 0)
        {
            length = _tokens.Length - _index;
        }

        var slice = _tokens.AsSpan().Slice(_index, length);
        _index += length;
        return new TokenStreamSlice(slice);
    }

    public TokenStreamSlice SliceNextBlock()
    {
        var length = 0;
        var lBraceCount = 0;
        var rBraceCount = 0;

        Token nextToken;

        do
        {
            nextToken = Peek(length);

            if (length > 0 && lBraceCount > 0 && lBraceCount == rBraceCount)
            {
                break;
            }

            if (nextToken.Match(TokenType.LBrace))
            {
                lBraceCount++;
            }
            else if (nextToken.Match(TokenType.RBrace))
            {
                rBraceCount++;
            }

            length++;
        }
        while (_index + length < _tokens.Length);

        return Slice(length);
    }
}

internal ref struct TokenStreamSlice
{
    private readonly ReadOnlySpan<Token> _tokens;
    private int _index;

    public readonly int Length => _tokens.Length;
    public readonly int Remaining => _tokens.Length - _index;
    public readonly bool HasMore => _index < _tokens.Length;

    public int Position
    {
        readonly get => _index;
        set
        {
            if (value < 0 || value > _tokens.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), "Position must be within the bounds of the token stream.");
            }
            _index = value;
        }
    }

    internal TokenStreamSlice(ReadOnlySpan<Token> tokens)
    {
        _tokens = tokens;
        _index = 0;
    }

    public Token Peek(int length = 0)
    {
        return TokenStreamImple.Peek(_tokens, _index, length);
    }

    public bool TryPeek(out Token token)
    {
        return TokenStreamImple.TryPeek(_tokens, _index, 0, out token);
    }

    public bool TryPeek(int length, out Token token)
    {
        return TokenStreamImple.TryPeek(_tokens, _index, length, out token);
    }

    public bool TryConsume(out Token token)
    {
        return TokenStreamImple.TryConsume(_tokens, ref _index, out token);
    }

    public Token Consume(int count = 1)
    {
        return TokenStreamImple.Consume(_tokens, ref _index, count);
    }

    public bool Match(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.Match(_tokens, _index, type, lexeme);
    }

    public int MatchMany(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.MatchMany(_tokens, _index, type, lexeme);
    }

    public Token Expect(TokenType type, string? lexeme = null)
    {
        return TokenStreamImple.Expect(_tokens, ref _index, type, lexeme);
    }

    public TokenStreamSlice Slice(int length = -1)
    {
        if (length <= 0)
        {
            length = _tokens.Length - _index;
        }

        var slice = _tokens.Slice(_index, length);
        _index += length;
        return new TokenStreamSlice(slice);
    }

    public TokenStreamSlice SliceNextBlock()
    {
        var length = 0;
        var lBraceCount = 0;
        var rBraceCount = 0;

        do
        {
            var nextToken = Peek(length);

            if (length > 0 && lBraceCount > 0 && lBraceCount == rBraceCount)
            {
                break;
            }

            if (nextToken.Match(TokenType.LBrace))
            {
                lBraceCount++;
            }
            else if (nextToken.Match(TokenType.RBrace))
            {
                rBraceCount++;
            }

            length++;
        }
        while (_index + length < _tokens.Length);

        if (lBraceCount != rBraceCount)
        {
            throw new InvalidOperationException("Unmatched braces in token stream.");
        }

        return Slice(length);
    }
}
