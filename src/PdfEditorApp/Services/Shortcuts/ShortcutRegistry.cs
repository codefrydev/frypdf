using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Services.Shortcuts;

/// <summary>
/// Central implementation of <see cref="IShortcutRegistry"/> managing declarative, customizable shortcuts
/// across host subsystems and plugins.
/// </summary>
public class ShortcutRegistry : IShortcutRegistry
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IUiSettingsService? _uiSettingsService;
    private readonly ConcurrentDictionary<string, ShortcutDescriptor> _shortcuts = new(StringComparer.OrdinalIgnoreCase);
    private readonly bool _isMacOS = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public event Action? ShortcutsChanged;

    public ShortcutRegistry(IServiceProvider? serviceProvider = null, IUiSettingsService? uiSettingsService = null)
    {
        _serviceProvider = serviceProvider ?? App.Services ?? new FallbackServiceProvider();
        _uiSettingsService = uiSettingsService;

        SeedHostShortcuts();
    }

    private sealed class FallbackServiceProvider : IServiceProvider
    {
        public object? GetService(Type serviceType) => null;
    }

    private void SeedHostShortcuts()
    {
        // 1. FILE & PROJECT
        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "file.new",
            Title = "New Document",
            Category = "File & Project",
            Description = "Create a new document from template or blank page",
            DefaultGesture = "Ctrl+N",
            MacGesture = "Cmd+N",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenNewDocumentDialogCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "file.open",
            Title = "Open Document",
            Category = "File & Project",
            Description = "Open an existing PDF project from disk",
            DefaultGesture = "Ctrl+O",
            MacGesture = "Cmd+O",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenProjectCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "file.save",
            Title = "Save Project",
            Category = "File & Project",
            Description = "Save active PDF creator project (.pdfproj)",
            DefaultGesture = "Ctrl+S",
            MacGesture = "Cmd+S",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.SaveProjectCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "file.export",
            Title = "Export to PDF",
            Category = "File & Project",
            Description = "Export active document layout to standard PDF",
            DefaultGesture = "Ctrl+E",
            MacGesture = "Cmd+E",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ExportPdfCommand.Execute(null)
        });

        // 2. EDIT OPERATIONS
        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.undo",
            Title = "Undo",
            Category = "Edit",
            Description = "Revert last canvas modification",
            DefaultGesture = "Ctrl+Z",
            MacGesture = "Cmd+Z",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.UndoCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.redo",
            Title = "Redo",
            Category = "Edit",
            Description = "Reapply previously undone modification",
            DefaultGesture = "Ctrl+Y",
            MacGesture = "Cmd+Shift+Z",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.RedoCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.copy",
            Title = "Copy Element",
            Category = "Edit",
            Description = "Copy selected canvas element to clipboard",
            DefaultGesture = "Ctrl+C",
            MacGesture = "Cmd+C",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.CopyCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.cut",
            Title = "Cut Element",
            Category = "Edit",
            Description = "Cut selected canvas element to clipboard",
            DefaultGesture = "Ctrl+X",
            MacGesture = "Cmd+X",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.CutCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.paste",
            Title = "Paste Element",
            Category = "Edit",
            Description = "Paste element from clipboard to canvas",
            DefaultGesture = "Ctrl+V",
            MacGesture = "Cmd+V",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.PasteCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "edit.duplicate",
            Title = "Duplicate Element",
            Category = "Edit",
            Description = "Duplicate selected element with offset",
            DefaultGesture = "Ctrl+D",
            MacGesture = "Cmd+D",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.DuplicateCommand.Execute(null)
        });

        // 3. VIEW & NAVIGATION
        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.toggleRibbon",
            Title = "Toggle Ribbon Collapse",
            Category = "View & Navigation",
            Description = "Collapse or expand top command ribbon",
            DefaultGesture = "Ctrl+F1",
            MacGesture = "Cmd+F1",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ToggleRibbonCollapseCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.toggleSidebar",
            Title = "Toggle Left Sidebar",
            Category = "View & Navigation",
            Description = "Show or hide page navigation sidebar",
            DefaultGesture = "Ctrl+B",
            MacGesture = "Cmd+B",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ToggleLeftSidebarCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.toggleInspector",
            Title = "Toggle Inspector Panel",
            Category = "View & Navigation",
            Description = "Expand or collapse right property inspector",
            DefaultGesture = "Ctrl+Shift+P",
            MacGesture = "Cmd+Shift+P",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ToggleInspectorCollapseCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.palette",
            Title = "Command Palette",
            Category = "View & Navigation",
            Description = "Open quick command and search palette",
            DefaultGesture = "Ctrl+K",
            MacGesture = "Cmd+K",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenCommandPaletteCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.find",
            Title = "Quick Search & Command",
            Category = "View & Navigation",
            Description = "Search tools, actions, and features",
            DefaultGesture = "Ctrl+F",
            MacGesture = "Cmd+F",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenCommandPaletteCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.settings",
            Title = "Preferences & Settings",
            Category = "View & Navigation",
            Description = "Open app customization and studio settings",
            DefaultGesture = "Ctrl+OemComma",
            MacGesture = "Cmd+OemComma",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.NavigateToSettingsCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.shortcuts",
            Title = "Shortcuts Cheatsheet",
            Category = "View & Navigation",
            Description = "View keyboard shortcuts cheatsheet modal",
            DefaultGesture = "F1",
            MacGesture = "F1",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenShortcutsHelpCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.ai",
            Title = "AI Assistant",
            Category = "View & Navigation",
            Description = "Open conversational AI document assistant",
            DefaultGesture = "Ctrl+I",
            MacGesture = "Cmd+I",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.OpenAiAssistantCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "view.theme",
            Title = "Toggle Dark/Light Theme",
            Category = "View & Navigation",
            Description = "Switch between dark and light appearance modes",
            DefaultGesture = "Ctrl+Shift+T",
            MacGesture = "Cmd+Shift+T",
            Scope = ShortcutScope.Global,
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ToggleThemeCommand.Execute(null)
        });

        // 4. ZOOM & CANVAS
        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "zoom.in",
            Title = "Zoom In",
            Category = "Zoom & Canvas",
            Description = "Increase document magnification",
            DefaultGesture = "Ctrl+OemPlus",
            MacGesture = "Cmd+OemPlus",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ZoomInCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "zoom.out",
            Title = "Zoom Out",
            Category = "Zoom & Canvas",
            Description = "Decrease document magnification",
            DefaultGesture = "Ctrl+OemMinus",
            MacGesture = "Cmd+OemMinus",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ZoomOutCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "zoom.reset",
            Title = "Reset Zoom (100%)",
            Category = "Zoom & Canvas",
            Description = "Reset canvas zoom to 100% scale",
            DefaultGesture = "Ctrl+D0",
            MacGesture = "Cmd+D0",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.ResetZoomCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "zoom.fitWidth",
            Title = "Fit to Width",
            Category = "Zoom & Canvas",
            Description = "Zoom page to fill available viewport width",
            DefaultGesture = "Ctrl+D1",
            MacGesture = "Cmd+D1",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.FitToWidthCommand.Execute(null)
        });

        RegisterShortcutInternal(new ShortcutDescriptor
        {
            Id = "zoom.fitPage",
            Title = "Fit to Page",
            Category = "Zoom & Canvas",
            Description = "Zoom whole page to fit visible viewport",
            DefaultGesture = "Ctrl+D9",
            MacGesture = "Cmd+D9",
            Scope = ShortcutScope.Workspace,
            ContextId = "PdfEditor",
            Action = sp => (sp.GetService<MainViewModel>() ?? _serviceProvider.GetService<MainViewModel>())?.FitToPageCommand.Execute(null)
        });
    }

    private void RegisterShortcutInternal(ShortcutDescriptor descriptor)
    {
        _shortcuts[descriptor.Id] = descriptor;
    }

    public IDisposable RegisterShortcut(ShortcutDescriptor descriptor)
    {
        ArgumentNullException.ThrowIfNull(descriptor);
        _shortcuts[descriptor.Id] = descriptor;
        ShortcutsChanged?.Invoke();

        return new DisposableAction(() =>
        {
            UnregisterShortcut(descriptor.Id);
        });
    }

    public bool UnregisterShortcut(string shortcutId)
    {
        if (_shortcuts.TryRemove(shortcutId, out _))
        {
            ShortcutsChanged?.Invoke();
            return true;
        }
        return false;
    }

    public IReadOnlyList<ShortcutDescriptor> GetAllShortcuts()
    {
        return _shortcuts.Values.OrderBy(s => s.Category).ThenBy(s => s.Title).ToList();
    }

    public ShortcutDescriptor? GetShortcut(string shortcutId)
    {
        _shortcuts.TryGetValue(shortcutId, out var descriptor);
        return descriptor;
    }

    public IReadOnlyList<ShortcutDescriptor> GetShortcutsByCategory(string category)
    {
        return _shortcuts.Values
            .Where(s => string.Equals(s.Category, category, StringComparison.OrdinalIgnoreCase))
            .OrderBy(s => s.Title)
            .ToList();
    }

    public string GetEffectiveGesture(string shortcutId)
    {
        if (!_shortcuts.TryGetValue(shortcutId, out var descriptor))
        {
            return string.Empty;
        }

        // 1. Check custom override in UI settings
        if (_uiSettingsService?.Settings.CustomShortcuts.TryGetValue(shortcutId, out var custom) == true &&
            !string.IsNullOrWhiteSpace(custom))
        {
            return custom;
        }

        // 2. macOS specific default gesture
        if (_isMacOS)
        {
            if (!string.IsNullOrWhiteSpace(descriptor.MacGesture))
            {
                return descriptor.MacGesture;
            }

            // Auto-map Ctrl to Cmd on macOS
            return NormalizeGestureForPlatform(descriptor.DefaultGesture, toMac: true);
        }

        // 3. Standard default gesture
        return NormalizeGestureForPlatform(descriptor.DefaultGesture, toMac: false);
    }

    public void SetCustomGesture(string shortcutId, string? newGesture)
    {
        if (!_shortcuts.TryGetValue(shortcutId, out var descriptor) || !descriptor.IsCustomizable)
        {
            return;
        }

        _uiSettingsService?.UpdateSettings(settings =>
        {
            if (string.IsNullOrWhiteSpace(newGesture) || string.Equals(newGesture, GetDefaultPlatformGesture(descriptor), StringComparison.OrdinalIgnoreCase))
            {
                settings.CustomShortcuts.Remove(shortcutId);
            }
            else
            {
                settings.CustomShortcuts[shortcutId] = newGesture.Trim();
            }
        });

        ShortcutsChanged?.Invoke();
    }

    public void ResetToDefault(string shortcutId)
    {
        SetCustomGesture(shortcutId, null);
    }

    public void ResetAllToDefaults()
    {
        _uiSettingsService?.UpdateSettings(settings =>
        {
            settings.CustomShortcuts.Clear();
        });

        ShortcutsChanged?.Invoke();
    }

    private string GetDefaultPlatformGesture(ShortcutDescriptor descriptor)
    {
        if (_isMacOS)
        {
            return !string.IsNullOrWhiteSpace(descriptor.MacGesture)
                ? descriptor.MacGesture
                : NormalizeGestureForPlatform(descriptor.DefaultGesture, toMac: true);
        }
        return NormalizeGestureForPlatform(descriptor.DefaultGesture, toMac: false);
    }

    private static string NormalizeGestureForPlatform(string gesture, bool toMac)
    {
        if (string.IsNullOrWhiteSpace(gesture)) return string.Empty;
        if (toMac)
        {
            return gesture.Replace("Ctrl+", "Cmd+", StringComparison.OrdinalIgnoreCase);
        }
        return gesture.Replace("Cmd+", "Ctrl+", StringComparison.OrdinalIgnoreCase);
    }

    public bool TryDispatch(string key, ShortcutModifiers modifiers, string? activeContextId = null)
    {
        var matchingDescriptors = FindMatchingDescriptors(key, modifiers, activeContextId);
        foreach (var desc in matchingDescriptors)
        {
            if (desc.CanExecute != null && !desc.CanExecute(_serviceProvider))
            {
                continue;
            }

            try
            {
                desc.Action?.Invoke(_serviceProvider);
                return true;
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("ShortcutRegistry", $"Failed executing shortcut action for '{desc.Id}' ({desc.Title})", ex);
            }
        }

        return false;
    }

    public bool TryDispatch(KeyEventArgs e, string? activeContextId = null)
    {
        ArgumentNullException.ThrowIfNull(e);

        var matchingDescriptors = FindMatchingDescriptors(e, activeContextId);
        foreach (var desc in matchingDescriptors)
        {
            if (desc.CanExecute != null && !desc.CanExecute(_serviceProvider))
            {
                continue;
            }

            try
            {
                desc.Action?.Invoke(_serviceProvider);
                e.Handled = true;
                return true;
            }
            catch (Exception ex)
            {
                AppLogService.Instance.LogError("ShortcutRegistry", $"Failed executing shortcut action for '{desc.Id}' ({desc.Title})", ex);
            }
        }

        return false;
    }

    private List<ShortcutDescriptor> FindMatchingDescriptors(KeyEventArgs e, string? activeContextId)
    {
        var result = new List<ShortcutDescriptor>();

        // Sort: Context-specific first, then Workspace-scoped matching activeContextId, then Global
        var candidates = _shortcuts.Values
            .OrderBy(s => s.Scope == ShortcutScope.Global ? 2 : 1)
            .ToList();

        foreach (var desc in candidates)
        {
            if (!IsScopeActive(desc, activeContextId))
            {
                continue;
            }

            var effectiveGesture = GetEffectiveGesture(desc.Id);
            if (string.IsNullOrWhiteSpace(effectiveGesture))
            {
                continue;
            }

            if (MatchesAvaloniaGesture(effectiveGesture, e))
            {
                result.Add(desc);
            }
        }

        return result;
    }

    private List<ShortcutDescriptor> FindMatchingDescriptors(string key, ShortcutModifiers modifiers, string? activeContextId)
    {
        var result = new List<ShortcutDescriptor>();

        var candidates = _shortcuts.Values
            .OrderBy(s => s.Scope == ShortcutScope.Global ? 2 : 1)
            .ToList();

        foreach (var desc in candidates)
        {
            if (!IsScopeActive(desc, activeContextId))
            {
                continue;
            }

            var effectiveGesture = GetEffectiveGesture(desc.Id);
            if (string.IsNullOrWhiteSpace(effectiveGesture))
            {
                continue;
            }

            if (MatchesStringGesture(effectiveGesture, key, modifiers))
            {
                result.Add(desc);
            }
        }

        return result;
    }

    private static bool IsScopeActive(ShortcutDescriptor desc, string? activeContextId)
    {
        if (desc.Scope == ShortcutScope.Global)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(activeContextId))
        {
            return desc.ContextId == null;
        }

        return string.Equals(desc.ContextId, activeContextId, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesAvaloniaGesture(string gestureString, KeyEventArgs e)
    {
        try
        {
            // Normalize for Avalonia KeyGesture parser
            var normalized = gestureString
                .Replace("Cmd+", "Meta+", StringComparison.OrdinalIgnoreCase);

            try
            {
                var gesture = KeyGesture.Parse(normalized);
                if (gesture != null && gesture.Matches(e))
                {
                    return true;
                }
            }
            catch
            {
                // Fall back to custom key string matching
            }

            // Fallback for special keys like Enter, Return, Plus, Minus
            if (MatchesCustomKeyString(gestureString, e.Key, e.KeyModifiers))
            {
                return true;
            }
        }
        catch
        {
            // Ignore parse errors on malformed gestures
        }

        return false;
    }

    private static bool MatchesStringGesture(string gestureString, string key, ShortcutModifiers modifiers)
    {
        // Parse parts: e.g. "Ctrl+Shift+F5" -> Ctrl, Shift, F5
        var parts = gestureString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var targetKey = parts[^1];
        var targetModifiers = ShortcutModifiers.None;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i];
            if (string.Equals(p, "Ctrl", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Control", StringComparison.OrdinalIgnoreCase))
                targetModifiers |= ShortcutModifiers.Control;
            else if (string.Equals(p, "Shift", StringComparison.OrdinalIgnoreCase))
                targetModifiers |= ShortcutModifiers.Shift;
            else if (string.Equals(p, "Alt", StringComparison.OrdinalIgnoreCase))
                targetModifiers |= ShortcutModifiers.Alt;
            else if (string.Equals(p, "Cmd", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Meta", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Win", StringComparison.OrdinalIgnoreCase))
                targetModifiers |= ShortcutModifiers.Meta;
        }

        if (!string.Equals(targetKey, key, StringComparison.OrdinalIgnoreCase))
            return false;

        if (modifiers == targetModifiers)
            return true;

        // Cross-platform tolerance: allow Control to match Meta and vice versa for primary command key
        if (modifiers.HasFlag(ShortcutModifiers.Control) && targetModifiers.HasFlag(ShortcutModifiers.Meta))
        {
            var testMods = (modifiers & ~ShortcutModifiers.Control) | ShortcutModifiers.Meta;
            if (testMods == targetModifiers) return true;
        }
        else if (modifiers.HasFlag(ShortcutModifiers.Meta) && targetModifiers.HasFlag(ShortcutModifiers.Control))
        {
            var testMods = (modifiers & ~ShortcutModifiers.Meta) | ShortcutModifiers.Control;
            if (testMods == targetModifiers) return true;
        }

        return false;
    }

    private static bool MatchesCustomKeyString(string gestureString, Key key, KeyModifiers modifiers)
    {
        var parts = gestureString.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return false;

        var targetKeyStr = parts[^1];
        var reqCtrl = false;
        var reqShift = false;
        var reqAlt = false;
        var reqMeta = false;

        for (int i = 0; i < parts.Length - 1; i++)
        {
            var p = parts[i];
            if (string.Equals(p, "Ctrl", StringComparison.OrdinalIgnoreCase)) reqCtrl = true;
            else if (string.Equals(p, "Shift", StringComparison.OrdinalIgnoreCase)) reqShift = true;
            else if (string.Equals(p, "Alt", StringComparison.OrdinalIgnoreCase)) reqAlt = true;
            else if (string.Equals(p, "Cmd", StringComparison.OrdinalIgnoreCase) || string.Equals(p, "Meta", StringComparison.OrdinalIgnoreCase)) reqMeta = true;
        }

        bool hasCtrl = modifiers.HasFlag(KeyModifiers.Control);
        bool hasShift = modifiers.HasFlag(KeyModifiers.Shift);
        bool hasAlt = modifiers.HasFlag(KeyModifiers.Alt);
        bool hasMeta = modifiers.HasFlag(KeyModifiers.Meta);

        if (reqCtrl != hasCtrl || reqShift != hasShift || reqAlt != hasAlt || reqMeta != hasMeta)
        {
            return false;
        }

        return KeyMatches(targetKeyStr, key);
    }

    private static bool KeyMatches(string targetKeyStr, Key key)
    {
        if (string.Equals(targetKeyStr, key.ToString(), StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        // Handle common symbol/number key aliases
        return (targetKeyStr, key) switch
        {
            ("Enter", Key.Enter) or ("Enter", Key.Return) => true,
            ("Return", Key.Enter) or ("Return", Key.Return) => true,
            ("=", Key.OemPlus) or ("+", Key.OemPlus) or ("Plus", Key.Add) => true,
            ("-", Key.OemMinus) or ("Minus", Key.Subtract) => true,
            ("0", Key.D0) or ("0", Key.NumPad0) => true,
            ("1", Key.D1) or ("1", Key.NumPad1) => true,
            ("9", Key.D9) or ("9", Key.NumPad9) => true,
            (",", Key.OemComma) => true,
            (".", Key.OemPeriod) => true,
            ("[", Key.OemOpenBrackets) or ("[", Key.Oem4) => true,
            ("]", Key.OemCloseBrackets) or ("]", Key.Oem6) => true,
            ("/", Key.OemQuestion) => true,
            _ => false
        };
    }

    private sealed class DisposableAction : IDisposable
    {
        private Action? _action;

        public DisposableAction(Action action)
        {
            _action = action;
        }

        public void Dispose()
        {
            var act = System.Threading.Interlocked.Exchange(ref _action, null);
            act?.Invoke();
        }
    }
}
