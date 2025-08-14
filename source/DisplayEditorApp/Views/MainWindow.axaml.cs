using Avalonia.Controls;
using Avalonia.Platform.Storage;
using DisplayEditorApp.ViewModels;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace DisplayEditorApp.Views;

/// <summary>
/// Main application window that hosts the DisplayEditor interface.
/// Provides file system operations (folder picking, CSV saving) to the MainViewModel.
/// Acts as the primary container for the MainView user control.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes the main window with fixed size constraints and dependency injection.
    /// Sets up the MainViewModel with required file operation delegates.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        
        // Set minimum window size constraints for proper UI layout
        MinWidth = 800;   // Ensures UI elements fit properly
        MinHeight = 620;  // Accommodates all controls and text display

        // Set default window size for optimal user experience
        Width = 800;     // Provides good workspace for file lists and content
        Height = 640;     // Allows comfortable viewing of text content

        WindowStartupLocation = WindowStartupLocation.CenterScreen;

        // Initialize MainViewModel with file operation dependencies
        // Passes local methods as delegates for folder picking and CSV saving
        DataContext = new MainViewModel(PickFolderAsync, SaveCsvFileAsync);
    }

    /// <summary>
    /// Opens a folder picker dialog for selecting the root data directory.
    /// Used by MainViewModel when user clicks "Select Folder" button.
    /// </summary>
    /// <returns>
    /// Local path of selected folder, or null if user cancels the dialog.
    /// Expected folder structure: selected folder should contain "zalmy" and "kancional" subdirectories.
    /// </returns>
    public async Task<string?> PickFolderAsync()
    {
        // Configure folder picker to allow single folder selection
        var folders = await this.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            AllowMultiple = false  // Only one root folder can be selected
        });

        // Return the local path of the first (and only) selected folder
        return folders.FirstOrDefault()?.Path.LocalPath;
    }

    /// <summary>
    /// Opens a save file dialog for exporting data to CSV format.
    /// Used by MainViewModel when user clicks "Export list" button in Simple mode.
    /// </summary>
    /// <returns>
    /// Local path where CSV file should be saved, or null if user cancels the dialog.
    /// File will contain exported data from all existing files in zalmy folder.
    /// </returns>
    public async Task<string?> SaveCsvFileAsync()
    {
        // Configure save dialog with CSV-specific options
        var file = await this.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save CSV File",              // Dialog title shown to user
            DefaultExtension = "csv",             // Automatically append .csv if not specified
            SuggestedFileName = "export.csv",     // Default filename in dialog
            FileTypeChoices = new[]
            {
                // Primary file type filter - CSV files
                new FilePickerFileType("CSV Files")
                {
                    Patterns = new[] { "*.csv" }  // Show only .csv files in filter
                },
                // Secondary option - all files (for advanced users)
                new FilePickerFileType("All Files")
                {
                    Patterns = new[] { "*.*" }    // Show all file types
                }
            }
        });

        // Return the local path where user wants to save the CSV file
        return file?.Path.LocalPath;
    }
}
