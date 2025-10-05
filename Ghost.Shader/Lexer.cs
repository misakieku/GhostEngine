namespace Ghost.Shader;

public class Lexer
{
    private readonly string _source;
    private int _pos = 0;
    private int _line = 0;
    private int _column = 0;

    public Lexer(string source) => _source = source;

    public IEnumerable<Token> Tokenize()
    {
        while (_pos < _source.Length)
        {
            var c = _source[_pos];

            // Skip whitespace
            if (char.IsWhiteSpace(c))
            {
                if (c == '\n')
                {
                    _line++;
                    _column = 0;
                }
                else
                {
                    _column++;
                }

                _pos++;
                continue;
            }

            // Punctuation
            switch (c)
            {
                case '=':
                    yield return MakeToken(TokenType.Equals, "=");
                    break;
                case ';':
                    yield return MakeToken(TokenType.Semicolon, ";");
                    break;
                case ',':
                    yield return MakeToken(TokenType.Comma, ",");
                    break;
                case '{':
                    yield return MakeToken(TokenType.LBrace, "{");
                    break;
                case '}':
                    yield return MakeToken(TokenType.RBrace, "}");
                    break;
                case '(':
                    yield return MakeToken(TokenType.LParen, "(");
                    break;
                case ')':
                    yield return MakeToken(TokenType.RParen, ")");
                    break;
                case '"':
                    yield return ReadString();
                    break;
                default:
                    if (char.IsLetter(c) || c == '_')
                    {
                        yield return ReadIdentifierOrKeyword();
                    }
                    else if (char.IsDigit(c) || c == '.')
                    {
                        yield return ReadNumber();
                    }
                    else
                    {
                        _pos++; // skip unknown
                    }
                    break;
            }
        }

        yield return new Token(TokenType.EndOfFile, "", _line, _column);
    }

    private Token MakeToken(TokenType type, string lexeme)
    {
        var token = new Token(type, lexeme, _line, _column);
        _pos++;
        _column++;
        return token;
    }

    private Token ReadString()
    {
        var startCol = _column;

        _pos++;
        _column++; // skip "

        var start = _pos;
        while (_pos < _source.Length && _source[_pos] != '"')
        {
            _pos++;
            _column++;
        }

        var text = _source.Substring(start, _pos - start);

        _pos++;
        _column++; // skip closing "

        return new Token(TokenType.StringLiteral, text, _line, startCol);
    }

    private Token ReadIdentifierOrKeyword()
    {
        var startCol = _column;
        var start = _pos;

        while (_pos < _source.Length && (char.IsLetterOrDigit(_source[_pos]) || _source[_pos] == '_'))
        {
            _pos++;
            _column++;
        }

        var text = _source.Substring(start, _pos - start);

        // Optional: detect keywords
        if (TokenLexicon.IsKeyword(text))
        {
            return new Token(TokenType.Keyword, text, _line, startCol);
        }

        return new Token(TokenType.Identifier, text, _line, startCol);
    }

    private Token ReadNumber()
    {
        var startCol = _column;
        var start = _pos;

        while (_pos < _source.Length && (char.IsDigit(_source[_pos]) || _source[_pos] == '.'))
        {
            _pos++;
            _column++;
        }

        var num = _source.Substring(start, _pos - start);
        return new Token(TokenType.Number, num, _line, startCol);
    }
}
