using System;
using System.Collections.Generic;

namespace PdfEditorApp.Services;

public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    event EventHandler? StateChanged;

    void RecordAction(string description, Action undoAction, Action redoAction);
    void Undo();
    void Redo();
    void Clear();
}

public class UndoRedoAction
{
    public string Description { get; set; } = "";
    public Action UndoAction { get; set; } = () => { };
    public Action RedoAction { get; set; } = () => { };
}

public class UndoRedoService : IUndoRedoService
{
    private readonly Stack<UndoRedoAction> _undoStack = new();
    private readonly Stack<UndoRedoAction> _redoStack = new();
    private const int MaxHistorySize = 50;

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public event EventHandler? StateChanged;

    public void RecordAction(string description, Action undoAction, Action redoAction)
    {
        if (_undoStack.Count >= MaxHistorySize)
        {
            var list = new List<UndoRedoAction>(_undoStack);
            list.RemoveAt(list.Count - 1);
            _undoStack.Clear();
            for (int i = list.Count - 1; i >= 0; i--)
            {
                _undoStack.Push(list[i]);
            }
        }

        _undoStack.Push(new UndoRedoAction
        {
            Description = description,
            UndoAction = undoAction,
            RedoAction = redoAction
        });

        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Undo()
    {
        if (_undoStack.Count == 0) return;

        var action = _undoStack.Pop();
        try
        {
            action.UndoAction();
        }
        catch { }

        _redoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Redo()
    {
        if (_redoStack.Count == 0) return;

        var action = _redoStack.Pop();
        try
        {
            action.RedoAction();
        }
        catch { }

        _undoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
