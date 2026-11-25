namespace Ghost.Editor.Core.Commands;

public interface IEditorCommand
{
    string Name { get; }

    ushort? TargetSceneId => null;

    void Execute();

    void Undo();

    void Redo() => Execute();

    bool CanMergeWith(IEditorCommand other) => false;

    void MergeWith(IEditorCommand other)
    {
    }
}
