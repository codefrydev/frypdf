using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing all standard Command Palette (⌘K / Ctrl+K) searchable actions.
/// </summary>
public class CommandPaletteBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.CommandPalette";
    public string Name => "Command Palette Actions Bundle";
    public string Description => "Extensible Command Palette (⌘K / Ctrl+K) and keyboard shortcuts for quick action navigation.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new FileCommandsPlugin(),
        new EditCommandsPlugin(),
        new InsertCommandsPlugin(),
        new ViewCommandsPlugin(),
        new SecurityCommandsPlugin()
    };
}

public class FileCommandsPlugin : IFryPlugin
{
    public string Id => "frypdf.commands.file";
    public string Name => "File Operations Commands";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.file.new",
            Title = "New Document / Templates",
            Subtitle = "Browse executive templates or start a blank document",
            Category = "File",
            IconKind = "FilePlusOutline",
            Shortcut = "⌘N",
            Order = 10
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.file.save",
            Title = "Save Project",
            Subtitle = "Save editable FryPDF project archive (.frypdf)",
            Category = "File",
            IconKind = "ContentSaveOutline",
            Shortcut = "⌘S",
            Order = 20
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.file.open",
            Title = "Open Project",
            Subtitle = "Open existing FryPDF project archive (.frypdf)",
            Category = "File",
            IconKind = "FolderOpenOutline",
            Shortcut = "⌘O",
            Order = 30
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.file.export",
            Title = "Export Production PDF",
            Subtitle = "Compile document to high-resolution vector PDF",
            Category = "File",
            IconKind = "FilePdfBox",
            Shortcut = "⌘E",
            Order = 40
        });

        return Task.CompletedTask;
    }
}

public class EditCommandsPlugin : IFryPlugin
{
    public string Id => "frypdf.commands.edit";
    public string Name => "Edit & History Commands";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.edit.undo",
            Title = "Undo Action",
            Subtitle = "Revert last canvas or page operation",
            Category = "Edit",
            IconKind = "Undo",
            Shortcut = "⌘Z",
            Order = 10
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.edit.redo",
            Title = "Redo Action",
            Subtitle = "Reapply reverted operation",
            Category = "Edit",
            IconKind = "Redo",
            Shortcut = "⌘Y",
            Order = 20
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.edit.cut",
            Title = "Cut Element",
            Subtitle = "Cut selected element to internal clipboard",
            Category = "Edit",
            IconKind = "ContentCut",
            Shortcut = "⌘X",
            Order = 30
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.edit.copy",
            Title = "Copy Element",
            Subtitle = "Copy selected element to internal clipboard",
            Category = "Edit",
            IconKind = "ContentCopy",
            Shortcut = "⌘C",
            Order = 40
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.edit.paste",
            Title = "Paste Element",
            Subtitle = "Paste element from clipboard to current page",
            Category = "Edit",
            IconKind = "ContentPaste",
            Shortcut = "⌘V",
            Order = 50
        });

        return Task.CompletedTask;
    }
}

public class InsertCommandsPlugin : IFryPlugin
{
    public string Id => "frypdf.commands.insert";
    public string Name => "Insert Elements Commands";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.insert.text",
            Title = "Insert Text Block",
            Subtitle = "Add multi-line editable rich text block",
            Category = "Insert",
            IconKind = "FormatColorText",
            Shortcut = "T",
            Order = 10
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.insert.image",
            Title = "Insert Image Graphic",
            Subtitle = "Import PNG, JPEG, or WebP graphic from disk",
            Category = "Insert",
            IconKind = "ImageOutline",
            Shortcut = "⌘I",
            Order = 20
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.insert.table",
            Title = "Insert Data Table",
            Subtitle = "Add customizable multi-column data grid",
            Category = "Insert",
            IconKind = "TableLarge",
            Order = 30
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.insert.math",
            Title = "Insert Math Equation (LaTeX)",
            Subtitle = "Add vector LaTeX mathematical equation",
            Category = "Insert",
            IconKind = "Sigma",
            Shortcut = "⌘M",
            Order = 40
        });

        return Task.CompletedTask;
    }
}

public class ViewCommandsPlugin : IFryPlugin
{
    public string Id => "frypdf.commands.view";
    public string Name => "View & Navigation Commands";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.view.ribbon",
            Title = "Toggle Ribbon Toolbar",
            Subtitle = "Collapse or expand top ribbon tools panel",
            Category = "View",
            IconKind = "ViewAgendaOutline",
            Shortcut = "⌘F1",
            Order = 10
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.view.sidebar",
            Title = "Toggle Pages Sidebar",
            Subtitle = "Collapse or expand left thumbnails & outline sidebar",
            Category = "View",
            IconKind = "DockLeft",
            Shortcut = "⌘B",
            Order = 20
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.view.inspector",
            Title = "Toggle Properties Inspector",
            Subtitle = "Collapse or expand right formatting & properties panel",
            Category = "View",
            IconKind = "DockRight",
            Shortcut = "⌘⇧P",
            Order = 30
        });

        return Task.CompletedTask;
    }
}

public class SecurityCommandsPlugin : IFryPlugin
{
    public string Id => "frypdf.commands.security";
    public string Name => "Security & Redaction Commands";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.security.sanitize",
            Title = "Sanitize Document",
            Subtitle = "Scrub author metadata and internal review notes",
            Category = "Security",
            IconKind = "ShieldCheck",
            Order = 10
        });

        ctx.RegisterCommand(new CommandPaletteDescriptor
        {
            Id = "cmd.security.redact",
            Title = "Search & Redact Pattern",
            Subtitle = "Auto-redact text occurrences on current page",
            Category = "Security",
            IconKind = "DatabaseSearchOutline",
            Order = 20
        });

        return Task.CompletedTask;
    }
}
