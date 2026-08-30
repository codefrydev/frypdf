using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace PdfEditorApp.Services;

public interface IUndoRedoService
{
    bool CanUndo { get; }
    bool CanRedo { get; }
    string? NextUndoDescription { get; }
    string? NextRedoDescription { get; }
    event EventHandler? StateChanged;

    void RecordAction(string description, Action undoAction, Action redoAction);
    string? Undo();
    string? Redo();
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
    private readonly LinkedList<UndoRedoAction> _undoList = new();
    private readonly Stack<UndoRedoAction> _redoStack = new();
    private const int MaxHistorySize = 100;

    public bool CanUndo => _undoList.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;

    public string? NextUndoDescription => _undoList.Last?.Value.Description;
    public string? NextRedoDescription => _redoStack.Count > 0 ? _redoStack.Peek().Description : null;

    public event EventHandler? StateChanged;

    public void RecordAction(string description, Action undoAction, Action redoAction)
    {
        if (_undoList.Count >= MaxHistorySize)
        {
            _undoList.RemoveFirst();
        }

        _undoList.AddLast(new UndoRedoAction
        {
            Description = description,
            UndoAction = undoAction,
            RedoAction = redoAction
        });

        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public string? Undo()
    {
        if (_undoList.Count == 0) return null;

        var node = _undoList.Last!;
        _undoList.RemoveLast();
        var action = node.Value;

        try
        {
            action.UndoAction();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UndoRedoService] Error during Undo '{action.Description}': {ex}");
        }

        _redoStack.Push(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return action.Description;
    }

    public string? Redo()
    {
        if (_redoStack.Count == 0) return null;

        var action = _redoStack.Pop();
        try
        {
            action.RedoAction();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UndoRedoService] Error during Redo '{action.Description}': {ex}");
        }

        _undoList.AddLast(action);
        StateChanged?.Invoke(this, EventArgs.Empty);
        return action.Description;
    }

    public void Clear()
    {
        _undoList.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
