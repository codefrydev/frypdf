using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using PdfEditorApp.Models;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views.Dialogs;

public partial class CommandPaletteDialog : UserControl
{
    public CommandPaletteDialog()
    {
        InitializeComponent();
        PropertyChanged += OnDialogPropertyChanged;
    }

    private void OnDialogPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == IsVisibleProperty && e.NewValue is true)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                SearchInputTextBox?.Focus();
                SearchInputTextBox?.SelectAll();
            }, Avalonia.Threading.DispatcherPriority.Loaded);
        }
    }

    private void OnBackdropPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.CloseCommandPalette();
        }
    }

    private void OnSearchInputKeyDown(object? sender, KeyEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        switch (e.Key)
        {
            case Key.Escape:
                vm.CloseCommandPalette();
                e.Handled = true;
                break;
            case Key.Enter:
                vm.ExecuteSelectedPaletteCommand();
                e.Handled = true;
                break;
            case Key.Down:
                vm.SelectNextPaletteCommand();
                e.Handled = true;
                break;
            case Key.Up:
                vm.SelectPreviousPaletteCommand();
                e.Handled = true;
                break;
        }
    }

    private void OnCommandItemPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control control && control.DataContext is CommandPaletteItem item)
        {
            if (DataContext is MainViewModel vm)
            {
                vm.CloseCommandPalette();
                item.Action?.Invoke();
                e.Handled = true;
            }
        }
    }
}
