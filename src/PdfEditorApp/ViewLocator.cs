using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp;

/// <summary>
/// Given a view model, returns the corresponding view if possible.
/// </summary>
[RequiresUnreferencedCode(
    "Default implementation of ViewLocator involves reflection which may be trimmed away.",
    Url = "https://docs.avaloniaui.net/docs/concepts/view-locator")]
public class ViewLocator : IDataTemplate
{
    /// <summary>
    /// View-model type → view type. The mapping is fixed for the life of the process, but this
    /// runs on every tool-page navigation, so the string manipulation and
    /// <see cref="Type.GetType(string)"/> lookup were repeated needlessly on the UI thread.
    /// </summary>
    /// <remarks>
    /// Only the type lookup is cached. Control instances cannot be shared between parents, so
    /// <see cref="Activator.CreateInstance(Type)"/> and the compiled-XAML load still run per view.
    /// </remarks>
    private static readonly ConcurrentDictionary<Type, (Type? ViewType, string ViewName)> ViewTypeCache = new();

    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var (viewType, viewName) = ViewTypeCache.GetOrAdd(param.GetType(), static vmType =>
        {
            var name = vmType.FullName!.Replace("ViewModel", "View", StringComparison.Ordinal);
            return (Type.GetType(name), name);
        });

        if (viewType != null)
        {
            return (Control)Activator.CreateInstance(viewType)!;
        }

        return new TextBlock { Text = "Not Found: " + viewName };
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
