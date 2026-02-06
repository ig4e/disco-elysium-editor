using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Service for managing undo/redo operations
/// </summary>
public partial class UndoRedoService : ObservableObject
{
    private readonly Stack<IUndoableCommand> _undoStack = new();
    private readonly Stack<IUndoableCommand> _redoStack = new();
    private const int MaxHistorySize = 100;

    [ObservableProperty]
    public partial bool CanUndo { get; set; }

    [ObservableProperty]
    public partial bool CanRedo { get; set; }

    [ObservableProperty]
    public partial string UndoDescription { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string RedoDescription { get; set; } = string.Empty;

    public void ExecuteCommand(IUndoableCommand command)
    {
        command.Execute();
        _undoStack.Push(command);
        _redoStack.Clear();

        // Limit history size
        if (_undoStack.Count > MaxHistorySize)
        {
            var temp = new List<IUndoableCommand>(_undoStack);
            temp.RemoveAt(temp.Count - 1);
            _undoStack.Clear();
            foreach (var cmd in temp)
            {
                _undoStack.Push(cmd);
            }
        }

        UpdateState();
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var command = _undoStack.Pop();
        command.Undo();
        _redoStack.Push(command);
        UpdateState();
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var command = _redoStack.Pop();
        command.Execute();
        _undoStack.Push(command);
        UpdateState();
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        UpdateState();
    }

    private void UpdateState()
    {
        CanUndo = _undoStack.Count > 0;
        CanRedo = _redoStack.Count > 0;
        UndoDescription = _undoStack.Count > 0 ? _undoStack.Peek().Description : string.Empty;
        RedoDescription = _redoStack.Count > 0 ? _redoStack.Peek().Description : string.Empty;
    }
}
