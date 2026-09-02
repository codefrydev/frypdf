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

    void RecordAction(string description, Action undoAction, Action redoAction, Action? onDiscarded = null);
    string? Undo();
    string? Redo();
    void Clear();
}

public class UndoRedoAction
{
    public string Description { get; set; } = "";
    public Action UndoAction { get; set; } = () => { };
    public Action RedoAction { get; set; } = () => { };

    /// <summary>
    /// Optional cleanup fired only when this action is discarded while it represents a
    /// deleted (currently off-canvas) element — capacity eviction from the undo list, or a
    /// full <see cref="UndoRedoService.Clear"/> on document teardown. Never fired for the
    /// automatic redo-stack clear on a new action, since a discarded redo-side delete means
    /// the element was already restored and is currently visible.
    /// </summary>
    public Action? OnDiscarded { get; set; }
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

    public void RecordAction(string description, Action undoAction, Action redoAction, Action? onDiscarded = null)
    {
        if (_undoList.Count >= MaxHistorySize)
        {
            DiscardAction(_undoList.First!.Value);
            _undoList.RemoveFirst();
        }

        _undoList.AddLast(new UndoRedoAction
        {
            Description = description,
            UndoAction = undoAction,
            RedoAction = redoAction,
            OnDiscarded = onDiscarded
        });

        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private static void DiscardAction(UndoRedoAction action)
    {
        try
        {
            action.OnDiscarded?.Invoke();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[UndoRedoService] Error during OnDiscarded '{action.Description}': {ex}");
        }
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
        // Document teardown: anything still holding a deleted element's resources (e.g. a
        // decoded chart/image bitmap) is about to become unreachable anyway, since the whole
        // document is being replaced — release it now instead of waiting on the GC. The redo
        // stack is intentionally left alone: any delete sitting there was already undone, so
        // the element it refers to is currently live elsewhere on the page.
        foreach (var action in _undoList)
        {
            DiscardAction(action);
        }

        _undoList.Clear();
        _redoStack.Clear();
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
