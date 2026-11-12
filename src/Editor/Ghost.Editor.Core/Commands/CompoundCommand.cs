namespace Ghost.Editor.Core.Commands;

public sealed class CompoundCommand : IEditorCommand
{
    private readonly IReadOnlyList<IEditorCommand> _commands;

    public string Name { get; }

    public CompoundCommand(string name, IReadOnlyList<IEditorCommand> commands)
    {
        Name = name;
        _commands = commands;
    }

    public void Execute()
    {
        for (var i = 0; i < _commands.Count; i++)
        {
            _commands[i].Execute();
        }
    }

    public void Undo()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            _commands[i].Undo();
        }
    }

    public void Redo()
    {
        for (var i = 0; i < _commands.Count; i++)
        {
            _commands[i].Redo();
        }
    }
}
