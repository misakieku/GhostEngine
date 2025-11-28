namespace Ghost.SDL.Compiler;

[Flags]
public enum TokenType
{
    None = 0,

    // Literals
    Identifier = 1 << 0,     // variable names, function names, etc.
    Keyword = 1 << 1,        // shader, properties, pipeline, pass, etc.
    Number = 1 << 2,         // numeric literals (123, 45.67, .5)
    StringLiteral = 1 << 3,  // "ForwardVS.hlsl"

    // Operators and Punctuation
    Equals = 1 << 4,         // =
    Semicolon = 1 << 5,      // ;
    Comma = 1 << 6,          // ,

    // Delimiters
    LBrace = 1 << 7,         // {
    RBrace = 1 << 8,         // }
    LParen = 1 << 9,         // (
    RParen = 1 << 10,        // )

    // Special
    EndOfFile = 1 << 11,     // EOF

    // Future extensions (commented out for now)
    // LBracket = 1 << 12,      // [
    // RBracket = 1 << 13,      // ]
    // Dot = 1 << 14,           // .
    // Colon = 1 << 15,         // :
    // Plus = 1 << 16,          // +
    // Minus = 1 << 17,         // -
    // Star = 1 << 18,          // *
    // Slash = 1 << 19,         // /
}

public readonly struct Token : IEquatable<Token>
{
    public readonly TokenType type;
    public readonly string lexeme;
    public readonly int line;
    public readonly int column;

    public static Token Null => new Token(TokenType.None, string.Empty, -1, -1);

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

    public bool Equals(Token other)
    {
        return type == other.type && lexeme == other.lexeme && line == other.line && column == other.column;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(type, lexeme, line, column);
    }

    public override bool Equals(object? obj)
    {
        return obj is Token token && Equals(token);
    }

    public static bool operator ==(Token left, Token right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Token left, Token right)
    {
        return !(left == right);
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

internal static class TokenLexicon
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
        public const string GLOBAL = "global";
        public const string LOCAL = "local";
    }

    public static class KnownPipelineProperties
    {
        public const string ZTEST = "ztest";
        public const string ZWRITE = "zwrite";
        public const string CULL = "cull";
        public const string BLEND = "blend";
        public const string COLORMASK = "color_mask";
    }

    public static class KnownFunctions
    {
        public const string TASK_SHADER = "ts";
        public const string MESH_SHADER = "ms";
        public const string PIXEL_SHADER = "ps";
        public const string COMPUTE_SHADER = "cs";
        public const string DYNAMIC = "dynamic";
        public const string STATIC = "static";
        public const string FALLBACK = "fallback";
    }

    public static class KnownTypes
    {
        // Basic types
        public const string FLOAT = "float";
        public const string FLOAT2 = "float2";
        public const string FLOAT3 = "float3";
        public const string FLOAT4 = "float4";
        public const string FLOAT4X4 = "float4x4";

        public const string INT = "int";
        public const string INT2 = "int2";
        public const string INT3 = "int3";
        public const string INT4 = "int4";

        public const string UINT = "uint";
        public const string UINT2 = "uint2";
        public const string UINT3 = "uint3";
        public const string UINT4 = "uint4";

        public const string BOOL = "bool";
        public const string BOOL2 = "bool2";
        public const string BOOL3 = "bool3";
        public const string BOOL4 = "bool4";

        // Texture types
        public const string TEXTURE2D = "tex2d";
        public const string TEXTURE2D_ARRAY = "tex2d_arr";
        public const string TEXTURE3D = "tex3d";
        public const string TEXTURECUBE = "texcube";
        public const string TEXTURECUBE_ARRAY = "texcube_arr";
    }

    public static class KnownTextureValue
    {
        public const string WHITE = "white";
        public const string BLACK = "black";
        public const string GREY = "grey";
        public const string NORMAL = "normal";
        public const string TRANSPARENT = "transparent";
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
        KnownKeywords.GLOBAL,
        KnownKeywords.LOCAL,
    };

    private static readonly HashSet<string> s_functions = new()
    {
        KnownFunctions.TASK_SHADER,
        KnownFunctions.PIXEL_SHADER,
        KnownFunctions.MESH_SHADER,
        KnownFunctions.COMPUTE_SHADER,
        KnownFunctions.DYNAMIC,
        KnownFunctions.STATIC,
    };

    private static readonly HashSet<string> s_types = new()
    {
        KnownTypes.FLOAT, KnownTypes.FLOAT2, KnownTypes.FLOAT3, KnownTypes.FLOAT4, KnownTypes.FLOAT4X4,
        KnownTypes.INT, KnownTypes.INT2, KnownTypes.INT3, KnownTypes.INT4,
        KnownTypes.UINT, KnownTypes.UINT2, KnownTypes.UINT3, KnownTypes.UINT4,
        KnownTypes.BOOL, KnownTypes.BOOL2, KnownTypes.BOOL3, KnownTypes.BOOL4,
        KnownTypes.TEXTURE2D, KnownTypes.TEXTURE2D_ARRAY, KnownTypes.TEXTURE3D,
        KnownTypes.TEXTURECUBE, KnownTypes.TEXTURECUBE_ARRAY,
    };

    private static readonly HashSet<string> s_textureDefaultValues = new()
    {
        KnownTextureValue.WHITE,
        KnownTextureValue.BLACK,
        KnownTextureValue.GREY,
        KnownTextureValue.NORMAL,
        KnownTextureValue.TRANSPARENT,
    };

    public static bool IsKeyword(string lexeme) => s_keywords.Contains(lexeme);

    public static bool IsFunction(string lexeme) => s_functions.Contains(lexeme);

    public static bool IsType(string lexeme) => s_types.Contains(lexeme);

    public static bool IsTextureDefaultValue(string lexeme) => s_textureDefaultValues.Contains(lexeme);
}
