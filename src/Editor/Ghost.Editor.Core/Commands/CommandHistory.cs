namespace Ghost.Editor.Core.Commands;

public sealed class CommandHistory
{
    private readonly List<IEditorCommand> _undoStack = new();
    private readonly List<IEditorCommand> _redoStack = new();
    private readonly int _maxCapacity;

    public static CommandHistory Instance { get; } = new(100);

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? NextUndoName => CanUndo ? _undoStack[^1].Name : null;
    public string? NextRedoName => CanRedo ? _redoStack[^1].Name : null;

    public event Action? HistoryChanged;

    public CommandHistory(int maxCapacity = 100)
    {
        _maxCapacity = maxCapacity;
    }

    public void Execute(IEditorCommand command)
    {
        if (CanUndo && _undoStack[^1].CanMergeWith(command))
        {
            _undoStack[^1].MergeWith(command);
            command.Execute();
            HistoryChanged?.Invoke();
            return;
        }

        command.Execute();
        _undoStack.Add(command);
        _redoStack.Clear();

        if (_undoStack.Count > _maxCapacity)
        {
            _undoStack.RemoveAt(0);
        }

        HistoryChanged?.Invoke();
    }

    public void Undo()
    {
        if (!CanUndo)
        {
            return;
        }

        var cmd = _undoStack[^1];
        _undoStack.RemoveAt(_undoStack.Count - 1);
        cmd.Undo();
        _redoStack.Add(cmd);

        HistoryChanged?.Invoke();
    }

    public void Redo()
    {
        if (!CanRedo)
        {
            return;
        }

        var cmd = _redoStack[^1];
        _redoStack.RemoveAt(_redoStack.Count - 1);
        cmd.Redo();
        _undoStack.Add(cmd);

        HistoryChanged?.Invoke();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }
}
