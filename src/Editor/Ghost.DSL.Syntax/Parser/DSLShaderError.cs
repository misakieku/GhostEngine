namespace Ghost.DSL.Parser;

public class DSLShaderError
{
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
    public string FilePath { get; set; } = string.Empty;

    public override string ToString() =>
        string.IsNullOrEmpty(FilePath)
            ? $"({Line},{Column}): {Message}"
            : $"{FilePath}({Line},{Column}): {Message}";
}
