namespace Ghost.DSL.ShaderCompiler.Parser;

internal interface IBlockParser<T, U>
{
    public static abstract bool ShouldEnter(Token token);
    public static abstract T? Parse(TokenStreamSlice ts);
    public static abstract U? SemanticAnalysis(T? syntax, List<DSLShaderError> errors);
}
