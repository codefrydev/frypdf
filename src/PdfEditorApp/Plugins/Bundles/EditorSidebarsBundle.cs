using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using PdfEditorApp.Core.Plugins;
using PdfEditorApp.Core.Plugins.Descriptors;
using PdfEditorApp.Core.Plugins.Profiles;

namespace PdfEditorApp.Plugins.Bundles;

/// <summary>
/// Plugin bundle providing left and right sidebar panels for the document editor workspace.
/// </summary>
public class EditorSidebarsBundle : IFryPluginBundle
{
    public string Id => "FryPdf.Bundle.EditorSidebars";
    public string Name => "Editor Sidebars Bundle";
    public string Description => "Pluggable sidebar panels for the document editor: Page Thumbnails, Bookmarks Outline, and Review Comments.";

    public IReadOnlyList<IFryPlugin> Plugins => new IFryPlugin[]
    {
        new ThumbnailsSidebarPlugin(),
        new OutlineSidebarPlugin(),
        new CommentsSidebarPlugin()
    };
}

public class ThumbnailsSidebarPlugin : IFryPlugin
{
    public string Id => "frypdf.sidebar.thumbnails";
    public string Name => "Page Thumbnails Sidebar";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterSidebarTab(new SidebarTabDescriptor
        {
            Id = "Thumbnails",
            Title = "Page Thumbnails",
            IconKind = "FileDocumentMultipleOutline",
            Tooltip = "Page Thumbnails & Reordering",
            Order = 10
        });
        return Task.CompletedTask;
    }
}

public class OutlineSidebarPlugin : IFryPlugin
{
    public string Id => "frypdf.sidebar.outline";
    public string Name => "Bookmarks & Outline Sidebar";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterSidebarTab(new SidebarTabDescriptor
        {
            Id = "Outline",
            Title = "Outline & Bookmarks",
            IconKind = "FormatListBulleted",
            Tooltip = "Document Outline & Navigation Bookmarks",
            Order = 20
        });
        return Task.CompletedTask;
    }
}

public class CommentsSidebarPlugin : IFryPlugin
{
    public string Id => "frypdf.sidebar.comments";
    public string Name => "Comments & Annotations Sidebar";
    public Version Version => new(1, 0, 0);
    public IReadOnlyList<Type> RequiredServices => Array.Empty<Type>();

    public Task ApplyAsync(IFryPluginContext ctx, CancellationToken ct = default)
    {
        ctx.RegisterSidebarTab(new SidebarTabDescriptor
        {
            Id = "Comments",
            Title = "Comments & Review",
            IconKind = "CommentTextMultipleOutline",
            Tooltip = "Review Annotations & Sticky Notes",
            Order = 30
        });
        return Task.CompletedTask;
    }
}
