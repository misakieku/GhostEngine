namespace Ghost.Editor.PoC.Commands;

public sealed class CommandHistory
{
    private readonly Stack<IEditorCommand> _undoStack = new();
    private readonly Stack<IEditorCommand> _redoStack = new();

    public int MaxHistory { get; set; } = 100;

    public event Action? HistoryChanged;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    public string? NextUndoName => CanUndo ? _undoStack.Peek().Name : null;
    public string? NextRedoName => CanRedo ? _redoStack.Peek().Name : null;

    public void Execute(IEditorCommand command)
    {
        command.Execute();
        _redoStack.Clear();

        if (_undoStack.Count > 0 && _undoStack.Peek().CanMergeWith(command))
        {
            _undoStack.Peek().MergeWith(command);
        }
        else
        {
            _undoStack.Push(command);
            TrimHistory();
        }

        HistoryChanged?.Invoke();
    }

    public bool Undo()
    {
        if (_undoStack.Count == 0)
        {
            return false;
        }

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        HistoryChanged?.Invoke();
        return true;
    }

    public bool Redo()
    {
        if (_redoStack.Count == 0)
        {
            return false;
        }

        var command = _redoStack.Pop();
        command.Redo();
        _undoStack.Push(command);
        HistoryChanged?.Invoke();
        return true;
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        HistoryChanged?.Invoke();
    }

    private void TrimHistory()
    {
        if (_undoStack.Count <= MaxHistory)
        {
            return;
        }

        var list = new List<IEditorCommand>(_undoStack);
        list.RemoveAt(list.Count - 1);
        _undoStack.Clear();
        for (var i = list.Count - 1; i >= 0; i--)
        {
            _undoStack.Push(list[i]);
        }
    }
}
