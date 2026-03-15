using System.Text;

namespace Ghost.NativeWrapperGen.Emit;

internal sealed class CodeWriter
{
    private readonly StringBuilder _builder = new();
    private int _indent;

    public string CurrentIndentString => new(' ', _indent * 4);

    public void WriteLine(string line = "")
    {
        if (line.Length == 0)
        {
            _builder.AppendLine();
            return;
        }

        _builder.Append(' ', _indent * 4);
        _builder.AppendLine(line);
    }

    public IDisposable IndentScope()
    {
        _indent++;
        return new Scope(this);
    }

    public override string ToString() => _builder.ToString();

    private sealed class Scope : IDisposable
    {
        private readonly CodeWriter _writer;

        public Scope(CodeWriter writer)
        {
            _writer = writer;
        }

        public void Dispose()
        {
            _writer._indent--;
        }
    }
}
