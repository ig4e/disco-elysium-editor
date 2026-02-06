namespace DiscoSaveEditor.Services;

/// <summary>
/// Represents a command that can be undone and redone
/// </summary>
public interface IUndoableCommand
{
    string Description { get; }
    void Execute();
    void Undo();
}
