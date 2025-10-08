namespace Ghost.Shader.Compiler.Parser;

internal interface IBlockParser<T, U>
{
    public static abstract bool ShouldEnter(Token token);
    public static abstract T? Parse(TokenStreamSlice ts);
    public static abstract U? SemanticAnalysis(T? syntax, List<ShaderError> errors);
}
