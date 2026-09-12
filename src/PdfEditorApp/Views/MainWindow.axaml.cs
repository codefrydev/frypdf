using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Microsoft.Extensions.DependencyInjection;
using PdfEditorApp.Core.Plugins.Loading;
using PdfEditorApp.Services;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class MainWindow : Window
{
    private bool _isShutdownCompleted;
    private bool _isClosingInProgress;

    public MainWindow()
    {
        InitializeComponent();
        MainViewModel.StorageProvider = StorageProvider;

        DataContextChanged += (s, e) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.DataStudio.StorageProvider = StorageProvider;
                vm.BatchGeneration.StorageProvider = StorageProvider;
            }
        };

        Closing += OnMainWindowClosing;

        AddHandler(KeyDownEvent, (sender, e) =>
        {
            if (DataContext is not MainViewModel vm) return;

            // Check if focus or event source is inside an active text input control
            var topLevel = TopLevel.GetTopLevel(this);
            var focused = topLevel?.FocusManager?.GetFocusedElement();

            bool isSourceTextBox = e.Source is TextBox ||
                                   e.Source is Avalonia.Controls.Presenters.TextPresenter ||
                                   (e.Source is Visual sv && sv.FindAncestorOfType<TextBox>() != null);

            bool isFocusedTextBox = focused is TextBox ||
                                    focused is Avalonia.Controls.Presenters.TextPresenter ||
                                    (focused is Visual fv && fv.FindAncestorOfType<TextBox>() != null);

            bool isInEditMode = vm.CurrentPage?.SelectedElement?.IsInEditMode == true;

            if (isSourceTextBox || isFocusedTextBox || isInEditMode)
            {
                bool isTextModifier = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);
                if (isTextModifier)
                {
                    switch (e.Key)
                    {
                        case Key.B:
                            vm.Inspector.ToggleBoldCommand.Execute(null);
                            e.Handled = true;
                            return;
                        case Key.I:
                            vm.Inspector.ToggleItalicCommand.Execute(null);
                            e.Handled = true;
                            return;
                        case Key.U:
                            vm.Inspector.ToggleUnderlineCommand.Execute(null);
                            e.Handled = true;
                            return;
                    }
                }
                return;
            }

            bool isCtrlOrCmd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Meta);

            // Handle Delete / Backspace when canvas elements are selected
            if ((e.Key == Key.Delete || e.Key == Key.Back) && !isCtrlOrCmd)
            {
                if (vm.CurrentPage != null && (vm.CurrentPage.SelectedElements.Count > 0 || vm.CurrentPage.SelectedElement != null))
                {
                    vm.Inspector.DeleteSelectedElementCommand.Execute(null);
                    e.Handled = true;
                    return;
                }
            }

            if (isCtrlOrCmd)
            {
                switch (e.Key)
                {
                    case Key.OemPlus:
                    case Key.Add:
                        vm.ZoomInCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.OemMinus:
                    case Key.Subtract:
                        vm.ZoomOutCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D0:
                    case Key.NumPad0:
                        vm.ResetZoomCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D1:
                    case Key.NumPad1:
                        vm.FitToWidthCommand.Execute(null);
                        e.Handled = true;
                        break;
                    case Key.D9:
                    case Key.NumPad9:
                        vm.FitToPageCommand.Execute(null);
                        e.Handled = true;
                        break;
                }
            }
        }, RoutingStrategies.Tunnel);
    }

    private async void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_isShutdownCompleted)
        {
            // Shutdown completed; hide window immediately and allow Avalonia to complete exit
            Hide();
            return;
        }

        // Intercept close to show full-screen saving loading progress and persist all work
        e.Cancel = true;

        if (_isClosingInProgress)
        {
            return;
        }
        _isClosingInProgress = true;

        try
        {
            await ExecuteGracefulShutdownAsync();
        }
        catch (Exception ex)
        {
            AppLogService.Instance.LogWarning("MainWindow", "Graceful shutdown encountered an error, proceeding with exit", ex);
        }
        finally
        {
            _isShutdownCompleted = true;
            Close();
        }
    }

    private async Task ExecuteGracefulShutdownAsync()
    {
        var loadingService = App.Services?.GetService<ILoadingProgressService>();
        ILoadingProgressHandle? handle = null;

        if (loadingService != null)
        {
            handle = loadingService.Show(new LoadingProgressOptions
            {
                Title = "Saving & Closing FryPDF Studio",
                Category = "SHUTDOWN & PERSISTENCE",
                StatusMessage = "Checking active workspace and document state...",
                PipelinePhases = new[] { "Inspect", "AutoSave", "Preferences", "Shutdown" },
                ActivePhaseIndex = 0,
                IsCancellable = false,
                ProgressPercent = 15.0
            });
        }

        try
        {
            if (DataContext is MainViewModel vm)
            {
                await vm.PrepareForShutdownAsync(handle);
            }
            else
            {
                handle?.UpdateStatus("Closing studio...", progressPercent: 100.0, activePhaseIndex: 3);
                await Task.Delay(200);
            }
        }
        finally
        {
            handle?.Dispose();
        }
    }
}