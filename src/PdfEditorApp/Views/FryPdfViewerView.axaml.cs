using System;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using PdfEditorApp.ViewModels.FryPdfViewer;

namespace PdfEditorApp.Views;

public partial class FryPdfViewerView : UserControl
{
    public FryPdfViewerView()
    {
        InitializeComponent();
        Loaded += (s, e) => Focus();
        PointerPressed += (s, e) => Focus();

        var scrollViewer = this.FindControl<ScrollViewer>("ViewportScrollViewer");
        if (scrollViewer != null)
        {
            scrollViewer.SizeChanged += (s, e) =>
            {
                if (DataContext is FryPdfViewerViewModel vm && vm.IsPresentationMode)
                {
                    vm.FitToViewport(e.NewSize.Width, e.NewSize.Height);
                }
            };
        }

        _scrollViewer = scrollViewer;

        DataContextChanged += OnViewerDataContextChanged;
    }

    private ScrollViewer? _scrollViewer;
    private FryPdfViewerViewModel? _subscribedViewModel;

    /// <summary>
    /// Re-points the view model subscription when the DataContext changes.
    /// </summary>
    /// <remarks>
    /// This used to add a *new* PropertyChanged lambda on every DataContext change without
    /// removing the previous one. The view model is long-lived (created once by MainViewModel),
    /// so the handlers accumulated on it, each capturing this view — the views were never
    /// collected and FitToViewport ran once per past subscription on every toggle.
    /// </remarks>
    private void OnViewerDataContextChanged(object? sender, EventArgs e)
    {
        if (_subscribedViewModel != null)
        {
            _subscribedViewModel.PropertyChanged -= OnViewerViewModelPropertyChanged;
            _subscribedViewModel = null;
        }

        if (DataContext is FryPdfViewerViewModel vm)
        {
            vm.PropertyChanged += OnViewerViewModelPropertyChanged;
            _subscribedViewModel = vm;
        }
    }

    private void OnViewerViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(FryPdfViewerViewModel.IsPresentationMode)) return;

        if (sender is FryPdfViewerViewModel vm && vm.IsPresentationMode && _scrollViewer != null)
        {
            vm.FitToViewport(_scrollViewer.Bounds.Width, _scrollViewer.Bounds.Height);
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        if (DataContext is not FryPdfViewerViewModel vm)
            return;

        // If focus is currently inside a text input or search bar, don't hijack typing
        if (TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is TextBox)
            return;

        if (e.Key == Key.Right || e.Key == Key.Down || e.Key == Key.PageDown || e.Key == Key.Space)
        {
            if (vm.CanGoNextPage)
            {
                vm.NextPage();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Left || e.Key == Key.Up || e.Key == Key.PageUp || e.Key == Key.Back)
        {
            if (vm.CanGoPreviousPage)
            {
                vm.PreviousPage();
                e.Handled = true;
            }
        }
        else if (e.Key == Key.Home)
        {
            vm.FirstPage();
            e.Handled = true;
        }
        else if (e.Key == Key.End)
        {
            vm.LastPage();
            e.Handled = true;
        }
        else if (e.Key == Key.Escape && vm.IsPresentationMode)
        {
            vm.IsPresentationMode = false;
            e.Handled = true;
        }
    }
}
