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

        DataContextChanged += (s, e) =>
        {
            if (DataContext is FryPdfViewerViewModel vm)
            {
                vm.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(FryPdfViewerViewModel.IsPresentationMode))
                    {
                        if (vm.IsPresentationMode && scrollViewer != null)
                        {
                            vm.FitToViewport(scrollViewer.Bounds.Width, scrollViewer.Bounds.Height);
                        }
                    }
                };
            }
        };
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
