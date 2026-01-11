namespace Ghost.RenderGraph.Concept;

internal static class ConsoleAPI
{
    [System.Diagnostics.Conditional("DEBUG")]
    public static void WriteLine()
    {
        Console.WriteLine();
    }

    [System.Diagnostics.Conditional("DEBUG")]
    public static void WriteLine(string? message)
    {
        Console.WriteLine(message);
    }
}
