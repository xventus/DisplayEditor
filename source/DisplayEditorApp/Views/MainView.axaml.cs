using Avalonia.Controls;
using Avalonia.Interactivity;
using DisplayEditorApp.ViewModels;
using System.Diagnostics;

namespace DisplayEditorApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        SimpleTextBoxBorder.SizeChanged += TextBox_SizeChanged;
        ExtendedTextBoxBorder.SizeChanged += TextBox_SizeChanged;

    }

    // Event handler pro změnu velikosti TextBox containeru
    private void TextBox_SizeChanged(object? sender, SizeChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // Aktualizujeme rozměry v ViewModelu
            viewModel.TextBoxActualWidth = e.NewSize.Width;
            viewModel.TextBoxActualHeight = e.NewSize.Height;

            Debug.WriteLine($"TextBox size changed: {e.NewSize.Width:F0}x{e.NewSize.Height:F0}");
        }
    }
}