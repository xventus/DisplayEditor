using CommunityToolkit.Mvvm.Input;
using DisplayEditorApp.Settings;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace DisplayEditorApp.ViewModels;

/// <summary>
/// ViewModel for the file editing dialog.
/// Manages file content loading, editing operations, and dialog state.
/// Handles both Simple and Extended mode configurations with different column/row limits.
/// Integrates with ContentOperation for file I/O operations using CP1250 encoding.
/// </summary>
public partial class EditDialogViewModel : ViewModelBase
{
    #region Properties

    /// <summary>
    /// Maximum number of columns for text display grid.
    /// Value depends on Simple/Extended mode selection.
    /// </summary>
    private int _maxColumn = 0;
    public int MaxColumns
    {
        get => _maxColumn;
        set => SetProperty(ref _maxColumn, value);
    }

    /// <summary>
    /// Maximum number of rows for text display grid.
    /// Value depends on Simple/Extended mode selection.
    /// </summary>
    private int _maxRows = 0;
    public int MaxRows
    {
        get => _maxRows;
        set => SetProperty(ref _maxRows, value);
    }

    /// <summary>
    /// Display name of the file being edited (filename only, without path).
    /// Used for dialog title and user interface display.
    /// </summary>
    private string _fileName = string.Empty;
    public string FileName
    {
        get => _fileName;
        set => SetProperty(ref _fileName, value);
    }

    /// <summary>
    /// Current content of the file being edited.
    /// Formatted according to MaxColumns/MaxRows constraints for editor display.
    /// Synchronized with the EditDialogView text controls.
    /// </summary>
    private string _fileContent = string.Empty;
    public string FileContent
    {
        get => _fileContent;
        set => SetProperty(ref _fileContent, value);
    }

    /// <summary>
    /// Full file system path to the file being edited.
    /// Used for loading and saving operations via ContentOperation.
    /// </summary>
    private string _filePath = string.Empty;
    public string FilePath
    {
        get => _filePath;
        set => SetProperty(ref _filePath, value);
    }

    /// <summary>
    /// Indicates whether the dialog was closed with a positive result (OK/Save).
    /// True if user saved changes, False if user cancelled.
    /// </summary>
    public bool DialogResult { get; private set; } = false;

    #endregion

    #region Events and Commands

    /// <summary>
    /// Event raised when the dialog should be closed.
    /// Parameter indicates the dialog result (true for OK, false for Cancel).
    /// Handled by EditDialogView to actually close the window.
    /// </summary>
    public event Action<bool>? RequestClose;

    /// <summary>
    /// Command for OK button - saves file content and closes dialog with positive result.
    /// Triggers file save operation via ContentOperation.SaveDataAsync.
    /// </summary>
    public ICommand OkCommand => new RelayCommand(OnOk);

    /// <summary>
    /// Command for Cancel button - closes dialog without saving changes.
    /// Discards any modifications made to file content.
    /// </summary>
    public ICommand CancelCommand => new RelayCommand(OnCancel);

    /// <summary>
    /// Legacy property for loaded event handling.
    /// Currently unused - kept for potential future extensibility.
    /// </summary>
    public Func<object, object, Task> Loaded { get; private set; }

    #endregion

    #region Constructors

    /// <summary>
    /// Parameterless constructor for design-time support.
    /// Creates empty ViewModel instance for XAML designer.
    /// </summary>
    public EditDialogViewModel()
    {
        // Initialize for design-time usage
        Loaded = null;
    }

    /// <summary>
    /// Main constructor for runtime usage.
    /// Initializes ViewModel with specific file and mode configuration.
    /// </summary>
    /// <param name="filePath">Full path to the file to be edited</param>
    /// <param name="simple">True for Simple mode (uses MaxColumns/MaxRows), False for Extended mode (uses MaxColumnsExt/MaxRowsExt)</param>
    public EditDialogViewModel(string filePath, bool simple)
    {
        FilePath = filePath;
        FileName = Path.GetFileName(filePath);

        // Configure grid constraints based on mode
        MaxColumns = simple ? GlobalSettings.MaxColumns : GlobalSettings.MaxColumnsExt;
        MaxRows = simple ? GlobalSettings.MaxRows : GlobalSettings.MaxRowsExt;

        Loaded = null;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Asynchronously loads file content from the specified file path.
    /// Uses ContentOperation to handle CP1250 encoding and format content for editor display.
    /// Updates FileContent property with loaded and formatted content.
    /// Called by EditDialogView when dialog is initialized.
    /// </summary>
    public async void LoadContentAsync()
    {
        FileContent = await ContentOperation.LoadDataAsync(FilePath, MaxColumns, MaxRows);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Handles OK button click - saves current content and closes dialog.
    /// Converts editor content back to file format and saves using CP1250 encoding.
    /// Sets positive dialog result and triggers dialog close.
    /// </summary>
    private async void OnOk()
    {
        // Save current content to file
        await ContentOperation.SaveDataAsync(FilePath, FileContent, MaxColumns, MaxRows);
        
        // Set positive result and close dialog
        DialogResult = true;
        RequestClose?.Invoke(true);
    }

    /// <summary>
    /// Handles Cancel button click - discards changes and closes dialog.
    /// Sets negative dialog result without saving any modifications.
    /// </summary>
    private void OnCancel()
    {
        // Set negative result and close dialog without saving
        DialogResult = false;
        RequestClose?.Invoke(false);
    }

    #endregion
}