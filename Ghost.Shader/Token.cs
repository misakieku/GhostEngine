namespace Ghost.Shader;

[Flags]
public enum TokenType
{
    None = 0,
    Identifier = 1 << 0,     // variable names, types, etc.
    Keyword = 1 << 1,       // shader, properties, pipeline, pass, etc.
    Number = 1 << 2,        // numeric literals
    StringLiteral = 1 << 3, // "ForwardVS.hlsl"
    Equals = 1 << 4,        // =
    Semicolon = 1 << 5,     // ;
    Comma = 1 << 6,         // ,
    LBrace = 1 << 7,        // {
    RBrace = 1 << 8,        // }
    LParen = 1 << 9,        // (
    RParen = 1 << 10,       // )
    EndOfFile = 1 << 11     // EOF
}

public readonly struct Token
{
    public readonly TokenType type;
    public readonly string lexeme;
    public readonly int line;
    public readonly int column;

    public Token(TokenType type, string lexeme, int line, int column)
    {
        this.type = type;
        this.lexeme = lexeme;
        this.line = line;
        this.column = column;
    }

    public override readonly string ToString()
    {
        return $"{type}('{lexeme}') at {line}:{column}";
    }
}

public static class TokenExtensions
{
    public static bool Match(this Token token, TokenType type, string? lexeme = null)
    {
        if (!type.HasFlag(token.type))
        {
            return false;
        }

        if (!string.IsNullOrEmpty(lexeme) && token.lexeme != lexeme)
        {
            return false;
        }

        return true;
    }

    public static void Expect(this Token token, TokenType type, string? lexeme = null)
    {
        if (!token.Match(type, lexeme))
        {
            var expected = lexeme != null ? $"{type}('{lexeme}')" : type.ToString();
            throw new Exception($"Unexpected token at line {token.line}, column {token.column}. Expected {expected}, got {token.type}('{token.lexeme}').");
        }
    }
}

public static class TokenLexicon
{
    public static class KnownKeywords
    {
        public const string SHADER = "shader";
        public const string PROPERTIES = "properties";
        public const string PIPELINE = "pipeline";
        public const string PASS = "pass";
        public const string DEFINES = "defines";
        public const string KEYWORDS = "keywords";
        public const string INCLUDES = "includes";
    }

    public static class KnownPipelineProperties
    {
        public const string ZTEST = "ztest";
        public const string ZWRITE = "zwrite";
        public const string CULL = "cull";
        public const string BLEND = "blend";
        public const string COLORMASK = "color_mask";
    }

    private static readonly HashSet<string> s_keywords = new()
    {
        KnownKeywords.SHADER,
        KnownKeywords.PROPERTIES,
        KnownKeywords.PIPELINE,
        KnownKeywords.PASS,
        KnownKeywords.DEFINES,
        KnownKeywords.KEYWORDS,
        KnownKeywords.INCLUDES,
    };

    private static readonly HashSet<string> s_function = new()
    {
        "vs",
        "ps",
        "ms",
        "cs",
        "dynamic",
        "static",
    };

    private static readonly HashSet<string> s_types = new()
    {
        "float",
        "float2",
        "float3",
        "float4",
        "int",
        "int2",
        "int3",
        "int4",
        "uint",
        "uint2",
        "uint3",
        "uint4",
        "bool",
        "bool2",
        "bool3",
        "bool4",
        "texture2d",
        "texture3d",
        "texturecube",
    };

    public static bool IsKeyword(string lexeme) => s_keywords.Contains(lexeme);
    public static bool IsFunction(string lexeme) => s_function.Contains(lexeme);
    public static bool IsType(string lexeme) => s_types.Contains(lexeme);
}
