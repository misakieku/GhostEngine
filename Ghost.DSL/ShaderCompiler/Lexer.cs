namespace Ghost.DSL.ShaderCompiler;

public class Lexer
{
    private readonly string _source;
    private int _pos = 0;
    private int _line = 1; // Lines typically start at 1
    private int _column = 1; // Columns typically start at 1

    public Lexer(string source)
    {
        _source = source;
    }

    public IEnumerable<Token> Tokenize()
    {
        while (!IsAtEnd())
        {
            var token = ScanToken();
            if (token != Token.Null)
            {
                yield return token;
            }
        }

        yield return new Token(TokenType.EndOfFile, string.Empty, _line, _column);
    }

    #region Core Scanning Logic

    private Token ScanToken()
    {
        var c = Consume();

        // Rule 1: Skip whitespace and handle line tracking
        if (char.IsWhiteSpace(c))
        {
            HandleWhitespace(c);
            return Token.Null;
        }

        // Rule 2: Handle comments
        if (c == '/')
        {
            if (HandleComments())
            {
                return Token.Null;
            }
            // If not a comment, fall through to handle '/' as an operator if needed
        }

        // Rule 3: Handle string literals
        if (c == '"')
        {
            return ScanStringLiteral();
        }

        // Rule 4: Handle numeric literals
        if (char.IsDigit(c) || (c == '.' && char.IsDigit(Peek())))
        {
            return ScanNumericLiteral(c);
        }

        // Rule 5: Handle identifiers and keywords
        if (char.IsLetter(c) || c == '_')
        {
            return ScanIdentifierOrKeyword(c);
        }

        // Rule 6: Handle single-character tokens (punctuation)
        var punctuationToken = ScanPunctuation(c);
        if (punctuationToken != Token.Null)
        {
            return punctuationToken;
        }

        // Rule 7: Skip unknown characters (could log warning in production)
        return Token.Null;
    }

    #endregion

    #region Rule Implementations

    private void HandleWhitespace(char c)
    {
        if (c == '\n')
        {
            _line++;
            _column = 1;
        }
        else if (c == '\r')
        {
            // Handle Windows line endings - peek for \n
            if (Peek() == '\n')
            {
                Consume();
            }

            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
    }

    private bool HandleComments()
    {
        var next = Peek();

        if (next == '/') // Single-line comment
        {
            return ScanSingleLineComment();
        }
        else if (next == '*') // Multi-line comment
        {
            return ScanMultiLineComment();
        }

        return false; // Not a comment
    }

    private bool ScanSingleLineComment()
    {
        // Skip the second '/'
        Consume();

        // Consume until end of line
        while (!IsAtEnd() && Peek() != '\n' && Peek() != '\r')
        {
            Consume();
        }

        return true;
    }

    private bool ScanMultiLineComment()
    {
        // Skip the '*'
        Consume();

        while (!IsAtEnd())
        {
            var c = Consume();

            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else if (c == '*' && Peek() == '/')
            {
                Consume(); // Consume closing '/'
                return true;
            }
            else
            {
                _column++;
            }
        }

        // Unclosed comment - could throw error in production
        return true;
    }

    private Token ScanStringLiteral()
    {
        var startLine = _line;
        var startColumn = _column - 1; // Account for opening quote
        var start = _pos;

        while (!IsAtEnd() && Peek() != '"')
        {
            var c = Peek();
            if (c == '\n')
            {
                _line++;
                _column = 1;
            }
            else if (c == '\\')
            {
                // Handle escape sequences
                Consume(); // Skip backslash
                if (!IsAtEnd())
                {
                    Consume();
                }
                continue;
            }

            Consume();
        }

        if (IsAtEnd())
        {
            // Unterminated string - could throw error in production
            var unterminatedText = _source[start.._pos];
            return new Token(TokenType.StringLiteral, unterminatedText, startLine, startColumn);
        }

        var text = _source[start.._pos];
        Consume(); // Consume closing quote

        return new Token(TokenType.StringLiteral, text, startLine, startColumn);
    }

    private Token ScanNumericLiteral(char firstChar)
    {
        var startColumn = _column - 1;
        var start = _pos - 1; // Include the first character

        var hasDot = firstChar == '.';

        while (!IsAtEnd())
        {
            var c = Peek();
            if (char.IsDigit(c))
            {
                Consume();
            }
            else if (c == '.' && !hasDot)
            {
                hasDot = true;
                Consume();
            }
            else
            {
                break;
            }
        }

        var number = _source[start.._pos];
        return new Token(TokenType.Number, number, _line, startColumn);
    }

    private Token ScanIdentifierOrKeyword(char firstChar)
    {
        var startColumn = _column - 1;
        var start = _pos - 1; // Include the first character

        while (!IsAtEnd() && (char.IsLetterOrDigit(Peek()) || Peek() == '_'))
        {
            Consume();
        }

        var text = _source[start.._pos];
        var tokenType = DetermineIdentifierType(text);

        return new Token(tokenType, text, _line, startColumn);
    }

    private Token ScanPunctuation(char c)
    {
        var startColumn = _column - 1;

        return c switch
        {
            '=' => new Token(TokenType.Equals, "=", _line, startColumn),
            ';' => new Token(TokenType.Semicolon, ";", _line, startColumn),
            ',' => new Token(TokenType.Comma, ",", _line, startColumn),
            '{' => new Token(TokenType.LBrace, "{", _line, startColumn),
            '}' => new Token(TokenType.RBrace, "}", _line, startColumn),
            '(' => new Token(TokenType.LParen, "(", _line, startColumn),
            ')' => new Token(TokenType.RParen, ")", _line, startColumn),
            _ => Token.Null // Unknown punctuation
        };
    }

    #endregion

    #region Classification Rules

    private static TokenType DetermineIdentifierType(string text)
    {
        // Rule: Check if it's a known keyword first
        if (TokenLexicon.IsKeyword(text))
        {
            return TokenType.Keyword;
        }

        // Rule: All other identifiers are treated as identifiers
        // (Could extend this to handle functions, types, etc. as separate token types)
        return TokenType.Identifier;
    }

    #endregion

    #region Helper Methods

    private bool IsAtEnd() => _pos >= _source.Length;

    private char Consume()
    {
        if (IsAtEnd())
            return '\0';

        var c = _source[_pos];
        _pos++;
        _column++;
        return c;
    }

    private char Peek() => IsAtEnd() ? '\0' : _source[_pos];

    private char PeekNext() => _pos + 1 >= _source.Length ? '\0' : _source[_pos + 1];

    #endregion
}
