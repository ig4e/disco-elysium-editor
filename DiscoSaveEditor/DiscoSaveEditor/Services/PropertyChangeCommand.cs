using System;

namespace DiscoSaveEditor.Services;

/// <summary>
/// Generic command for undoing property changes
/// </summary>
/// <typeparam name="T">The type of the property value</typeparam>
public class PropertyChangeCommand<T> : IUndoableCommand
{
    private readonly Action<T> _setter;
    private readonly T _oldValue;
    private readonly T _newValue;

    public string Description { get; }

    public PropertyChangeCommand(string description, Action<T> setter, T oldValue, T newValue)
    {
        Description = description;
        _setter = setter;
        _oldValue = oldValue;
        _newValue = newValue;
    }

    public void Execute()
    {
        _setter(_newValue);
    }

    public void Undo()
    {
        _setter(_oldValue);
    }
}
