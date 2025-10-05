namespace Ghost.Shader.ParserBlock;

internal interface IBlockParser<T>
{
    public static abstract bool ShouldEnter(Token token);

    public static abstract T Parse(TokenStreamSlice ts);
}

internal interface IBlockParser<T, U> : IBlockParser<T>
{
    public U SemanticAnalysis(T syntax, List<ShaderError> errors);
}
