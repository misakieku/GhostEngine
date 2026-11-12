namespace Ghost.Editor.Core.Commands;

public interface IEditorCommand
{
    string Name { get; }

    void Execute();

    void Undo();

    void Redo() => Execute();

    bool CanMergeWith(IEditorCommand other) => false;

    void MergeWith(IEditorCommand other)
    {
    }
}
