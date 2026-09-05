using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using PdfEditorApp.ViewModels;

namespace PdfEditorApp.Views;

public partial class HomeView : UserControl
{
    public HomeView()
    {
        InitializeComponent();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is HomeViewModel vm)
        {
            vm.PropertyChanged -= OnHomeViewModelPropertyChanged;
            vm.PropertyChanged += OnHomeViewModelPropertyChanged;
        }
    }

    private void OnHomeViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(HomeViewModel.SelectedNavSection))
        {
            if (MainContentScrollViewer != null)
            {
                MainContentScrollViewer.Offset = new Vector(0, 0);
            }
        }
    }
}
