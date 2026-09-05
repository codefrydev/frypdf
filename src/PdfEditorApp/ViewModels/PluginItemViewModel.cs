using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins;

namespace PdfEditorApp.ViewModels;

/// <summary>
/// Presentation model for an active or loaded plugin in the FryPDF Plugin & Extension Studio.
/// </summary>
public partial class PluginItemViewModel : ViewModelBase
{
    private bool _isToggling;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _version = "1.0.0";

    [ObservableProperty]
    private string _category = "General";

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _iconKind = "PuzzleOutline";

    [ObservableProperty]
    private string _iconColorHex = "#7C3AED";

    [ObservableProperty]
    private bool _isActive = true;

    [ObservableProperty]
    private bool _isExternal;

    [ObservableProperty]
    private string _sourceAssembly = "Built-in";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSettings))]
    private System.Collections.Generic.IReadOnlyDictionary<string, Core.Plugins.Manifests.PluginSettingDefinition>? _settingsSchema;

    public bool HasSettings => SettingsSchema != null && SettingsSchema.Count > 0;

    /// <summary>
    /// Optional custom handler for toggling active status.
    /// </summary>
    public Func<string, bool, Task>? ToggleHandler { get; set; }

    partial void OnIsActiveChanged(bool value)
    {
        if (_isToggling) return;
        _ = HandleToggleAsync(value);
    }

    private async Task HandleToggleAsync(bool active)
    {
        if (string.IsNullOrWhiteSpace(Id)) return;

        _isToggling = true;
        try
        {
            if (ToggleHandler != null)
            {
                await ToggleHandler(Id, active);
            }
            else
            {
                var host = App.Services?.GetService<PluginHost>();
                if (host != null)
                {
                    if (active)
                    {
                        await host.EnablePluginAsync(Id);
                    }
                    else
                    {
                        await host.DisablePluginAsync(Id);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[PluginItemViewModel] Toggle failed for '{Id}': {ex.Message}");
            _isToggling = true;
            IsActive = !active;
            _isToggling = false;
        }
        finally
        {
            _isToggling = false;
        }
    }
}
